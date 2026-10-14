using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace RapidOCRSchedule.Excel;

/// <summary>
/// Writes simplified schedules as Office Open XML workbooks.
/// </summary>
public static class ScheduleExcelExporter
{
    private const int MaximumDataRowCount = 1_048_575;
    private const string SpreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string PackageRelationshipsNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string OfficeRelationshipsNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

    private static readonly DateOnly MinimumExcelDate = new(1900, 1, 1);

    private static readonly string[] Headers =
    [
        "日期",
        "星期",
        "岗位一（队长）",
        "岗位二",
        "岗位三",
        "岗位四",
    ];

    /// <summary>
    /// Exports <paramref name="rows"/> to an XLSX file, replacing an existing file at the same path.
    /// Rows are sorted by date; weekend rows are retained.
    /// </summary>
    /// <param name="filePath">Destination path, including the .xlsx file name.</param>
    /// <param name="rows">Schedule rows to export.</param>
    public static void Export(string filePath, IEnumerable<ExportRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        IReadOnlyList<ExportRow> preparedRows = PrepareRows(rows);
        string destinationPath = Path.GetFullPath(filePath);
        string? directory = Path.GetDirectoryName(destinationPath);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"The destination directory does not exist: {directory}");
        }

        string temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(destinationPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (FileStream stream = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                WritePackage(stream, preparedRows, leaveOpen: true);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    /// <summary>
    /// Exports <paramref name="rows"/> to a writable stream as an XLSX workbook.
    /// Rows are sorted by date; weekend rows are retained.
    /// </summary>
    /// <param name="destination">Writable destination stream.</param>
    /// <param name="rows">Schedule rows to export.</param>
    /// <param name="leaveOpen">
    /// <see langword="true"/> to keep <paramref name="destination"/> open after export;
    /// otherwise, <see langword="false"/>.
    /// </param>
    public static void Export(Stream destination, IEnumerable<ExportRow> rows, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (!destination.CanWrite)
        {
            throw new ArgumentException("The destination stream must be writable.", nameof(destination));
        }

        if (destination.CanSeek)
        {
            destination.Position = 0;
            destination.SetLength(0);
        }

        IReadOnlyList<ExportRow> preparedRows = PrepareRows(rows);
        WritePackage(destination, preparedRows, leaveOpen);
    }

    private static IReadOnlyList<ExportRow> PrepareRows(IEnumerable<ExportRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        List<ExportRow> result = [];
        HashSet<DateOnly> dates = [];
        int sourceIndex = 0;

        foreach (ExportRow? row in rows)
        {
            if (row is null)
            {
                throw new ArgumentException($"Row at index {sourceIndex} is null.", nameof(rows));
            }

            sourceIndex++;

            if (row.Date < MinimumExcelDate)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rows),
                    row.Date,
                    $"Excel dates must be on or after {MinimumExcelDate:yyyy-MM-dd}.");
            }

            if (!dates.Add(row.Date))
            {
                throw new ArgumentException(
                    $"More than one row was supplied for {row.Date:yyyy-MM-dd}.",
                    nameof(rows));
            }

            ValidateCellText(row.Weekday, sourceIndex - 1, nameof(row.Weekday));
            ValidateCellText(row.PositionOneLeader, sourceIndex - 1, nameof(row.PositionOneLeader));
            ValidateCellText(row.PositionTwo, sourceIndex - 1, nameof(row.PositionTwo));
            ValidateCellText(row.PositionThree, sourceIndex - 1, nameof(row.PositionThree));
            ValidateCellText(row.PositionFour, sourceIndex - 1, nameof(row.PositionFour));

            result.Add(row);

            if (result.Count > MaximumDataRowCount)
            {
                throw new ArgumentException(
                    $"An Excel worksheet can contain at most {MaximumDataRowCount:N0} data rows when a header row is present.",
                    nameof(rows));
            }
        }

        result.Sort(static (left, right) => left.Date.CompareTo(right.Date));
        return result;
    }

    private static void ValidateCellText(string? value, int rowIndex, string propertyName)
    {
        if (value is null)
        {
            return;
        }

        if (value.Length > 32_767)
        {
            throw new ArgumentException(
                $"{propertyName} at row index {rowIndex} exceeds Excel's 32,767-character cell limit.",
                "rows");
        }

        try
        {
            XmlConvert.VerifyXmlChars(value);
        }
        catch (XmlException exception)
        {
            throw new ArgumentException(
                $"{propertyName} at row index {rowIndex} contains a character that is not valid in XML.",
                "rows",
                exception);
        }
    }

    private static void WritePackage(Stream destination, IReadOnlyList<ExportRow> rows, bool leaveOpen)
    {
        using ZipArchive archive = new(destination, ZipArchiveMode.Create, leaveOpen);

        WriteContentTypes(archive);
        WriteRootRelationships(archive);
        WriteWorkbook(archive);
        WriteWorkbookRelationships(archive);
        WriteStyles(archive);
        WriteWorksheet(archive, rows);
    }

    private static void WriteContentTypes(ZipArchive archive)
    {
        using XmlWriter writer = CreateXmlWriter(archive, "[Content_Types].xml");

        writer.WriteStartDocument();
        writer.WriteStartElement("Types", "http://schemas.openxmlformats.org/package/2006/content-types");
        WriteContentTypeDefault(writer, "rels", "application/vnd.openxmlformats-package.relationships+xml");
        WriteContentTypeDefault(writer, "xml", "application/xml");
        WriteContentTypeOverride(writer, "/xl/workbook.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
        WriteContentTypeOverride(writer, "/xl/worksheets/sheet1.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");
        WriteContentTypeOverride(writer, "/xl/styles.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml");
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteContentTypeDefault(XmlWriter writer, string extension, string contentType)
    {
        writer.WriteStartElement("Default");
        writer.WriteAttributeString("Extension", extension);
        writer.WriteAttributeString("ContentType", contentType);
        writer.WriteEndElement();
    }

    private static void WriteContentTypeOverride(XmlWriter writer, string partName, string contentType)
    {
        writer.WriteStartElement("Override");
        writer.WriteAttributeString("PartName", partName);
        writer.WriteAttributeString("ContentType", contentType);
        writer.WriteEndElement();
    }

    private static void WriteRootRelationships(ZipArchive archive)
    {
        using XmlWriter writer = CreateXmlWriter(archive, "_rels/.rels");

        writer.WriteStartDocument();
        writer.WriteStartElement("Relationships", PackageRelationshipsNamespace);
        WriteRelationship(
            writer,
            "rId1",
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument",
            "xl/workbook.xml");
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteWorkbook(ZipArchive archive)
    {
        using XmlWriter writer = CreateXmlWriter(archive, "xl/workbook.xml");

        writer.WriteStartDocument();
        writer.WriteStartElement("workbook", SpreadsheetNamespace);
        writer.WriteAttributeString("xmlns", "r", null, OfficeRelationshipsNamespace);
        writer.WriteStartElement("bookViews");
        writer.WriteStartElement("workbookView");
        writer.WriteAttributeString("activeTab", "0");
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteStartElement("sheets");
        writer.WriteStartElement("sheet");
        writer.WriteAttributeString("name", "排班表");
        writer.WriteAttributeString("sheetId", "1");
        writer.WriteAttributeString("r", "id", OfficeRelationshipsNamespace, "rId1");
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteWorkbookRelationships(ZipArchive archive)
    {
        using XmlWriter writer = CreateXmlWriter(archive, "xl/_rels/workbook.xml.rels");

        writer.WriteStartDocument();
        writer.WriteStartElement("Relationships", PackageRelationshipsNamespace);
        WriteRelationship(
            writer,
            "rId1",
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet",
            "worksheets/sheet1.xml");
        WriteRelationship(
            writer,
            "rId2",
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles",
            "styles.xml");
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteRelationship(XmlWriter writer, string id, string type, string target)
    {
        writer.WriteStartElement("Relationship");
        writer.WriteAttributeString("Id", id);
        writer.WriteAttributeString("Type", type);
        writer.WriteAttributeString("Target", target);
        writer.WriteEndElement();
    }

    private static void WriteStyles(ZipArchive archive)
    {
        using XmlWriter writer = CreateXmlWriter(archive, "xl/styles.xml");

        writer.WriteStartDocument();
        writer.WriteStartElement("styleSheet", SpreadsheetNamespace);

        writer.WriteStartElement("numFmts");
        writer.WriteAttributeString("count", "1");
        writer.WriteStartElement("numFmt");
        writer.WriteAttributeString("numFmtId", "164");
        writer.WriteAttributeString("formatCode", "m/d");
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteStartElement("fonts");
        writer.WriteAttributeString("count", "2");
        WriteFont(writer, bold: false);
        WriteFont(writer, bold: true);
        writer.WriteEndElement();

        writer.WriteStartElement("fills");
        writer.WriteAttributeString("count", "2");
        WritePatternFill(writer, "none");
        WritePatternFill(writer, "gray125");
        writer.WriteEndElement();

        writer.WriteStartElement("borders");
        writer.WriteAttributeString("count", "1");
        writer.WriteStartElement("border");
        writer.WriteElementString("left", string.Empty);
        writer.WriteElementString("right", string.Empty);
        writer.WriteElementString("top", string.Empty);
        writer.WriteElementString("bottom", string.Empty);
        writer.WriteElementString("diagonal", string.Empty);
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteStartElement("cellStyleXfs");
        writer.WriteAttributeString("count", "1");
        WriteXf(writer, numberFormatId: 0, fontId: 0, applyNumberFormat: false, applyFont: false, center: false, includeXfId: false);
        writer.WriteEndElement();

        writer.WriteStartElement("cellXfs");
        writer.WriteAttributeString("count", "3");
        WriteXf(writer, numberFormatId: 0, fontId: 0, applyNumberFormat: false, applyFont: false, center: false);
        WriteXf(writer, numberFormatId: 164, fontId: 0, applyNumberFormat: true, applyFont: false, center: false);
        WriteXf(writer, numberFormatId: 0, fontId: 1, applyNumberFormat: false, applyFont: true, center: true);
        writer.WriteEndElement();

        writer.WriteStartElement("cellStyles");
        writer.WriteAttributeString("count", "1");
        writer.WriteStartElement("cellStyle");
        writer.WriteAttributeString("name", "Normal");
        writer.WriteAttributeString("xfId", "0");
        writer.WriteAttributeString("builtinId", "0");
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteStartElement("dxfs");
        writer.WriteAttributeString("count", "0");
        writer.WriteEndElement();
        writer.WriteStartElement("tableStyles");
        writer.WriteAttributeString("count", "0");
        writer.WriteAttributeString("defaultTableStyle", "TableStyleMedium2");
        writer.WriteAttributeString("defaultPivotStyle", "PivotStyleLight16");
        writer.WriteEndElement();

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteFont(XmlWriter writer, bool bold)
    {
        writer.WriteStartElement("font");
        if (bold)
        {
            writer.WriteElementString("b", string.Empty);
        }

        writer.WriteStartElement("sz");
        writer.WriteAttributeString("val", "11");
        writer.WriteEndElement();
        writer.WriteStartElement("name");
        writer.WriteAttributeString("val", "Calibri");
        writer.WriteEndElement();
        writer.WriteStartElement("family");
        writer.WriteAttributeString("val", "2");
        writer.WriteEndElement();
        writer.WriteStartElement("scheme");
        writer.WriteAttributeString("val", "minor");
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WritePatternFill(XmlWriter writer, string patternType)
    {
        writer.WriteStartElement("fill");
        writer.WriteStartElement("patternFill");
        writer.WriteAttributeString("patternType", patternType);
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteXf(
        XmlWriter writer,
        int numberFormatId,
        int fontId,
        bool applyNumberFormat,
        bool applyFont,
        bool center,
        bool includeXfId = true)
    {
        writer.WriteStartElement("xf");
        writer.WriteAttributeString("numFmtId", numberFormatId.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("fontId", fontId.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("fillId", "0");
        writer.WriteAttributeString("borderId", "0");
        if (includeXfId)
        {
            writer.WriteAttributeString("xfId", "0");
        }

        if (applyNumberFormat)
        {
            writer.WriteAttributeString("applyNumberFormat", "1");
        }

        if (applyFont)
        {
            writer.WriteAttributeString("applyFont", "1");
        }

        if (center)
        {
            writer.WriteAttributeString("applyAlignment", "1");
            writer.WriteStartElement("alignment");
            writer.WriteAttributeString("horizontal", "center");
            writer.WriteAttributeString("vertical", "center");
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteWorksheet(ZipArchive archive, IReadOnlyList<ExportRow> rows)
    {
        using XmlWriter writer = CreateXmlWriter(archive, "xl/worksheets/sheet1.xml");

        int lastRowNumber = rows.Count + 1;

        writer.WriteStartDocument();
        writer.WriteStartElement("worksheet", SpreadsheetNamespace);
        writer.WriteStartElement("dimension");
        writer.WriteAttributeString("ref", $"A1:F{lastRowNumber}");
        writer.WriteEndElement();

        writer.WriteStartElement("sheetViews");
        writer.WriteStartElement("sheetView");
        writer.WriteAttributeString("workbookViewId", "0");
        writer.WriteStartElement("pane");
        writer.WriteAttributeString("ySplit", "1");
        writer.WriteAttributeString("topLeftCell", "A2");
        writer.WriteAttributeString("activePane", "bottomLeft");
        writer.WriteAttributeString("state", "frozen");
        writer.WriteEndElement();
        writer.WriteStartElement("selection");
        writer.WriteAttributeString("pane", "bottomLeft");
        writer.WriteAttributeString("activeCell", "A2");
        writer.WriteAttributeString("sqref", "A2");
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteStartElement("sheetFormatPr");
        writer.WriteAttributeString("defaultRowHeight", "15");
        writer.WriteEndElement();
        WriteColumns(writer);

        writer.WriteStartElement("sheetData");
        WriteHeaderRow(writer);
        for (int index = 0; index < rows.Count; index++)
        {
            WriteDataRow(writer, index + 2, rows[index]);
        }

        writer.WriteEndElement();

        writer.WriteStartElement("autoFilter");
        writer.WriteAttributeString("ref", $"A1:F{lastRowNumber}");
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteColumns(XmlWriter writer)
    {
        writer.WriteStartElement("cols");
        WriteColumn(writer, 1, 12);
        WriteColumn(writer, 2, 12);
        WriteColumn(writer, 3, 18);
        WriteColumn(writer, 4, 16);
        WriteColumn(writer, 5, 16);
        WriteColumn(writer, 6, 16);
        writer.WriteEndElement();
    }

    private static void WriteColumn(XmlWriter writer, int index, int width)
    {
        writer.WriteStartElement("col");
        writer.WriteAttributeString("min", index.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("max", index.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("width", width.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("customWidth", "1");
        writer.WriteEndElement();
    }

    private static void WriteHeaderRow(XmlWriter writer)
    {
        writer.WriteStartElement("row");
        writer.WriteAttributeString("r", "1");
        writer.WriteAttributeString("ht", "22");
        writer.WriteAttributeString("customHeight", "1");

        for (int columnIndex = 0; columnIndex < Headers.Length; columnIndex++)
        {
            WriteInlineStringCell(writer, $"{GetColumnName(columnIndex)}1", Headers[columnIndex], styleIndex: 2);
        }

        writer.WriteEndElement();
    }

    private static void WriteDataRow(XmlWriter writer, int rowNumber, ExportRow row)
    {
        writer.WriteStartElement("row");
        writer.WriteAttributeString("r", rowNumber.ToString(CultureInfo.InvariantCulture));

        WriteNumberCell(writer, $"A{rowNumber}", GetExcelSerialNumber(row.Date), styleIndex: 1);
        WriteInlineStringCell(writer, $"B{rowNumber}", row.Weekday);
        WriteInlineStringCell(writer, $"C{rowNumber}", row.PositionOneLeader);
        WriteInlineStringCell(writer, $"D{rowNumber}", row.PositionTwo);
        WriteInlineStringCell(writer, $"E{rowNumber}", row.PositionThree);
        WriteInlineStringCell(writer, $"F{rowNumber}", row.PositionFour);

        writer.WriteEndElement();
    }

    private static void WriteNumberCell(XmlWriter writer, string reference, double value, int styleIndex)
    {
        writer.WriteStartElement("c");
        writer.WriteAttributeString("r", reference);
        writer.WriteAttributeString("s", styleIndex.ToString(CultureInfo.InvariantCulture));
        writer.WriteElementString("v", value.ToString("0.##########", CultureInfo.InvariantCulture));
        writer.WriteEndElement();
    }

    private static void WriteInlineStringCell(XmlWriter writer, string reference, string? value, int? styleIndex = null)
    {
        writer.WriteStartElement("c");
        writer.WriteAttributeString("r", reference);
        writer.WriteAttributeString("t", "inlineStr");
        if (styleIndex.HasValue)
        {
            writer.WriteAttributeString("s", styleIndex.Value.ToString(CultureInfo.InvariantCulture));
        }

        writer.WriteStartElement("is");
        writer.WriteStartElement("t");
        writer.WriteAttributeString("xml", "space", XmlNamespace, "preserve");
        writer.WriteString(value ?? string.Empty);
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static double GetExcelSerialNumber(DateOnly date)
    {
        double serialNumber = date.ToDateTime(TimeOnly.MinValue).ToOADate();

        // OLE Automation and Excel differ before Excel's fictitious 1900-02-29.
        return date < new DateOnly(1900, 3, 1) ? serialNumber - 1 : serialNumber;
    }

    private static string GetColumnName(int zeroBasedIndex) => ((char)('A' + zeroBasedIndex)).ToString();

    private static XmlWriter CreateXmlWriter(ZipArchive archive, string entryName)
    {
        ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        Stream stream = entry.Open();
        return XmlWriter.Create(
            stream,
            new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                CloseOutput = true,
                Indent = false,
                OmitXmlDeclaration = false,
            });
    }
}
