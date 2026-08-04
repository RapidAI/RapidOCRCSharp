namespace RapidOCRSchedule.Excel;

/// <summary>
/// Represents one row in an exported workday schedule.
/// </summary>
/// <param name="Date">The calendar date for the row.</param>
/// <param name="Weekday">The weekday text displayed in Excel.</param>
/// <param name="PositionOneLeader">The name or text for position one (leader).</param>
/// <param name="PositionTwo">The name or text for position two.</param>
/// <param name="PositionThree">The name or text for position three.</param>
/// <param name="PositionFour">The name or text for position four.</param>
public sealed record ExportRow(
    DateOnly Date,
    string? Weekday,
    string? PositionOneLeader,
    string? PositionTwo,
    string? PositionThree,
    string? PositionFour)
{
    /// <summary>
    /// Creates an export row and derives its Chinese weekday text from <paramref name="date"/>.
    /// </summary>
    public ExportRow(
        DateOnly date,
        string? positionOneLeader,
        string? positionTwo,
        string? positionThree,
        string? positionFour)
        : this(
            date,
            GetChineseWeekday(date.DayOfWeek),
            positionOneLeader,
            positionTwo,
            positionThree,
            positionFour)
    {
    }

    private static string GetChineseWeekday(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => "星期一",
        DayOfWeek.Tuesday => "星期二",
        DayOfWeek.Wednesday => "星期三",
        DayOfWeek.Thursday => "星期四",
        DayOfWeek.Friday => "星期五",
        DayOfWeek.Saturday => "星期六",
        DayOfWeek.Sunday => "星期日",
        _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek)),
    };
}
