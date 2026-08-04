using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using RapidOCRSchedule.Excel;

namespace RapidOCRSchedule.Tests;

public sealed class ScheduleExcelExporterTests
{
    private static readonly XNamespace SpreadsheetNamespace =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    [Fact]
    public void Export_CreatesRequiredOoxmlPackageParts()
    {
        using MemoryStream destination = ExportToOpenStream();
        using ZipArchive archive = new(destination, ZipArchiveMode.Read);

        string[] expectedParts =
        [
            "[Content_Types].xml",
            "_rels/.rels",
            "xl/_rels/workbook.xml.rels",
            "xl/styles.xml",
            "xl/workbook.xml",
            "xl/worksheets/sheet1.xml",
        ];

        string[] actualParts = archive.Entries
            .Select(entry => entry.FullName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            expectedParts.OrderBy(name => name, StringComparer.Ordinal),
            actualParts);
    }

    [Fact]
    public void Export_WritesHeadersNumericDateWithMonthDayStyleAndSafeUnicodeText()
    {
        DateOnly date = new(2026, 8, 3);
        ExportRow row = new(
            date,
            "星期一 & <早班>",
            " 张三 & <队长> ",
            "李四 \"二号\"",
            "王五 / 研发",
            "赵六 😀");

        using MemoryStream destination = ExportToOpenStream(row);
        XDocument worksheet = LoadPart(destination, "xl/worksheets/sheet1.xml");
        XDocument styles = LoadPart(destination, "xl/styles.xml");

        XElement headerRow = worksheet
            .Descendants(SpreadsheetNamespace + "row")
            .Single(element => (string?)element.Attribute("r") == "1");
        string[] headers = headerRow
            .Elements(SpreadsheetNamespace + "c")
            .Select(ReadInlineText)
            .ToArray();

        Assert.Equal(
            ["日期", "星期", "岗位一（队长）", "岗位二", "岗位三", "岗位四"],
            headers);

        XElement dateCell = GetCell(worksheet, "A2");
        Assert.Null(dateCell.Attribute("t"));
        Assert.Equal("1", (string?)dateCell.Attribute("s"));
        double serialNumber = double.Parse(
            dateCell.Element(SpreadsheetNamespace + "v")!.Value,
            CultureInfo.InvariantCulture);
        Assert.Equal(date.ToDateTime(TimeOnly.MinValue).ToOADate(), serialNumber);

        XElement numberFormat = styles
            .Descendants(SpreadsheetNamespace + "numFmt")
            .Single(element => (string?)element.Attribute("numFmtId") == "164");
        Assert.Equal("m/d", (string?)numberFormat.Attribute("formatCode"));

        XElement[] cellFormats = styles.Root!
            .Element(SpreadsheetNamespace + "cellXfs")!
            .Elements(SpreadsheetNamespace + "xf")
            .ToArray();
        XElement dateFormat = cellFormats[int.Parse((string)dateCell.Attribute("s")!, CultureInfo.InvariantCulture)];
        Assert.Equal("164", (string?)dateFormat.Attribute("numFmtId"));
        Assert.Equal("1", (string?)dateFormat.Attribute("applyNumberFormat"));

        Assert.Equal("星期一 & <早班>", ReadInlineText(GetCell(worksheet, "B2")));
        Assert.Equal(" 张三 & <队长> ", ReadInlineText(GetCell(worksheet, "C2")));
        Assert.Equal("李四 \"二号\"", ReadInlineText(GetCell(worksheet, "D2")));
        Assert.Equal("王五 / 研发", ReadInlineText(GetCell(worksheet, "E2")));
        Assert.Equal("赵六 😀", ReadInlineText(GetCell(worksheet, "F2")));
        Assert.Equal(
            "preserve",
            (string?)GetCell(worksheet, "C2")
                .Descendants(SpreadsheetNamespace + "t")
                .Single()
                .Attribute(XNamespace.Xml + "space"));
    }

    [Fact]
    public void Export_FiltersWeekendsAndSortsRemainingRowsByDate()
    {
        ExportRow[] rows =
        [
            CreateRow(new DateOnly(2026, 8, 2), "Sunday"),
            CreateRow(new DateOnly(2026, 8, 7), "Friday"),
            CreateRow(new DateOnly(2026, 8, 1), "Saturday"),
            CreateRow(new DateOnly(2026, 8, 4), "Tuesday"),
            CreateRow(new DateOnly(2026, 8, 3), "Monday"),
        ];

        using MemoryStream destination = ExportToOpenStream(rows);
        XDocument worksheet = LoadPart(destination, "xl/worksheets/sheet1.xml");
        XElement[] dataRows = worksheet
            .Descendants(SpreadsheetNamespace + "row")
            .Where(element => (string?)element.Attribute("r") != "1")
            .ToArray();

        DateOnly[] exportedDates = dataRows
            .Select(row => ReadDate(row.Elements(SpreadsheetNamespace + "c").Single(
                cell => ((string?)cell.Attribute("r"))?.StartsWith('A') == true)))
            .ToArray();
        string[] exportedNames = dataRows
            .Select(row => ReadInlineText(row.Elements(SpreadsheetNamespace + "c").Single(
                cell => ((string?)cell.Attribute("r"))?.StartsWith('C') == true)))
            .ToArray();

        Assert.Equal(
            [new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 4), new DateOnly(2026, 8, 7)],
            exportedDates);
        Assert.Equal(["Monday", "Tuesday", "Friday"], exportedNames);
    }

    [Fact]
    public void Export_ThrowsWhenAWorkdayDateIsDuplicated()
    {
        DateOnly duplicateDate = new(2026, 8, 3);
        ExportRow[] rows =
        [
            CreateRow(duplicateDate, "first"),
            CreateRow(duplicateDate, "second"),
        ];
        using MemoryStream destination = new();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => ScheduleExcelExporter.Export(destination, rows, leaveOpen: true));

        Assert.Equal("rows", exception.ParamName);
        Assert.Contains("2026-08-03", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Export_WithLeaveOpenTrue_KeepsDestinationUsable()
    {
        using MemoryStream destination = new();

        ScheduleExcelExporter.Export(destination, Array.Empty<ExportRow>(), leaveOpen: true);

        Assert.True(destination.CanWrite);
        destination.Position = 0;
        using ZipArchive archive = new(destination, ZipArchiveMode.Read, leaveOpen: true);
        Assert.NotNull(archive.GetEntry("xl/worksheets/sheet1.xml"));
    }

    [Fact]
    public void Export_ToPrefilledSeekableStream_TruncatesOldContentAndCreatesValidPackage()
    {
        using MemoryStream destination = new(new byte[200_000], writable: true);
        destination.Position = 0;

        ScheduleExcelExporter.Export(destination, [CreateRow(new DateOnly(2026, 8, 3), "张三")], leaveOpen: true);

        Assert.True(destination.Length < 200_000);
        destination.Position = 0;
        using ZipArchive archive = new(destination, ZipArchiveMode.Read, leaveOpen: true);
        Assert.NotNull(archive.GetEntry("xl/worksheets/sheet1.xml"));
    }

    [Fact]
    public void Export_WritesCorrectExcelSerialsAcross1900LeapYearBoundary()
    {
        ExportRow[] rows =
        [
            CreateRow(new DateOnly(1900, 3, 1), "March"),
            CreateRow(new DateOnly(1900, 1, 1), "January"),
            CreateRow(new DateOnly(1900, 2, 28), "February"),
        ];

        using MemoryStream destination = ExportToOpenStream(rows);
        XDocument worksheet = LoadPart(destination, "xl/worksheets/sheet1.xml");

        Assert.Equal("1", GetCell(worksheet, "A2").Element(SpreadsheetNamespace + "v")!.Value);
        Assert.Equal("59", GetCell(worksheet, "A3").Element(SpreadsheetNamespace + "v")!.Value);
        Assert.Equal("61", GetCell(worksheet, "A4").Element(SpreadsheetNamespace + "v")!.Value);
    }

    [Fact]
    public void Export_StyleXfOmitsCellFormatReference()
    {
        using MemoryStream destination = ExportToOpenStream();
        XDocument styles = LoadPart(destination, "xl/styles.xml");

        XElement styleXf = styles.Root!
            .Element(SpreadsheetNamespace + "cellStyleXfs")!
            .Element(SpreadsheetNamespace + "xf")!;
        Assert.Null(styleXf.Attribute("xfId"));
    }

    [Fact]
    public void Export_WithDefaultLeaveOpenFalse_DisposesDestination()
    {
        MemoryStream destination = new();

        ScheduleExcelExporter.Export(destination, Array.Empty<ExportRow>());

        Assert.False(destination.CanWrite);
        Assert.Throws<ObjectDisposedException>(() => destination.WriteByte(0));
    }

    private static MemoryStream ExportToOpenStream(params ExportRow[] rows)
    {
        MemoryStream destination = new();
        ScheduleExcelExporter.Export(destination, rows, leaveOpen: true);
        destination.Position = 0;
        return destination;
    }

    private static XDocument LoadPart(Stream workbook, string partName)
    {
        workbook.Position = 0;
        using ZipArchive archive = new(workbook, ZipArchiveMode.Read, leaveOpen: true);
        ZipArchiveEntry? entry = archive.GetEntry(partName);
        Assert.NotNull(entry);
        using Stream part = entry.Open();
        return XDocument.Load(part);
    }

    private static XElement GetCell(XDocument worksheet, string reference) =>
        worksheet
            .Descendants(SpreadsheetNamespace + "c")
            .Single(element => (string?)element.Attribute("r") == reference);

    private static string ReadInlineText(XElement cell) =>
        cell
            .Element(SpreadsheetNamespace + "is")!
            .Element(SpreadsheetNamespace + "t")!
            .Value;

    private static DateOnly ReadDate(XElement cell)
    {
        double serialNumber = double.Parse(
            cell.Element(SpreadsheetNamespace + "v")!.Value,
            CultureInfo.InvariantCulture);
        return DateOnly.FromDateTime(DateTime.FromOADate(serialNumber));
    }

    private static ExportRow CreateRow(DateOnly date, string positionOne) =>
        new(date, positionOne, null, null, null);
}
