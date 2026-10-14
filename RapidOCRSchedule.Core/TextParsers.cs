using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace RapidOCRSchedule.Core;

public static partial class DateTextParser
{
    [GeneratedRegex(@"^\s*(?<month>\d{1,2})\s*(?:[/\-.]|月)\s*(?<day>\d{1,2})\s*(?:日|号)?\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex MonthDayRegex();

    public static bool TryParseMonthDay(string? value, out int month, out int day)
    {
        month = 0;
        day = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = Normalize(value);
        Match match = MonthDayRegex().Match(normalized);
        if (!match.Success ||
            !int.TryParse(match.Groups["month"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out month) ||
            !int.TryParse(match.Groups["day"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out day) ||
            month is < 1 or > 12 || day is < 1 or > 31)
        {
            month = 0;
            day = 0;
            return false;
        }

        return true;
    }

    public static bool TryCreateDate(int year, string? value, out DateOnly date)
    {
        date = default;
        if (!TryParseMonthDay(value, out int month, out int day))
        {
            return false;
        }

        try
        {
            date = new DateOnly(year, month, day);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    public static string Normalize(string value) =>
        value.Normalize(NormalizationForm.FormKC)
            .Replace('／', '/')
            .Replace('－', '-')
            .Replace('—', '-')
            .Trim();
}

public static class WeekdayTextParser
{
    private static readonly IReadOnlyDictionary<string, DayOfWeek> Values =
        new Dictionary<string, DayOfWeek>(StringComparer.OrdinalIgnoreCase)
        {
            ["星期一"] = DayOfWeek.Monday,
            ["周一"] = DayOfWeek.Monday,
            ["礼拜一"] = DayOfWeek.Monday,
            ["星期二"] = DayOfWeek.Tuesday,
            ["周二"] = DayOfWeek.Tuesday,
            ["礼拜二"] = DayOfWeek.Tuesday,
            ["星期三"] = DayOfWeek.Wednesday,
            ["周三"] = DayOfWeek.Wednesday,
            ["礼拜三"] = DayOfWeek.Wednesday,
            ["星期四"] = DayOfWeek.Thursday,
            ["周四"] = DayOfWeek.Thursday,
            ["礼拜四"] = DayOfWeek.Thursday,
            ["星期五"] = DayOfWeek.Friday,
            ["周五"] = DayOfWeek.Friday,
            ["礼拜五"] = DayOfWeek.Friday,
            ["星期六"] = DayOfWeek.Saturday,
            ["周六"] = DayOfWeek.Saturday,
            ["礼拜六"] = DayOfWeek.Saturday,
            ["星期日"] = DayOfWeek.Sunday,
            ["星期天"] = DayOfWeek.Sunday,
            ["周日"] = DayOfWeek.Sunday,
            ["周天"] = DayOfWeek.Sunday,
            ["礼拜日"] = DayOfWeek.Sunday,
            ["礼拜天"] = DayOfWeek.Sunday,
        };

    public static bool TryParse(string? value, out DayOfWeek weekday)
    {
        weekday = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Normalize(NormalizationForm.FormKC)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim();
        return Values.TryGetValue(normalized, out weekday);
    }

    public static string ToChinese(DayOfWeek weekday) => weekday switch
    {
        DayOfWeek.Monday => "星期一",
        DayOfWeek.Tuesday => "星期二",
        DayOfWeek.Wednesday => "星期三",
        DayOfWeek.Thursday => "星期四",
        DayOfWeek.Friday => "星期五",
        DayOfWeek.Saturday => "星期六",
        DayOfWeek.Sunday => "星期日",
        _ => throw new ArgumentOutOfRangeException(nameof(weekday)),
    };
}
