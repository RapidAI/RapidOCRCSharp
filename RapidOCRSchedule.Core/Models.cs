namespace RapidOCRSchedule.Core;

public readonly record struct OcrBounds(float Left, float Top, float Right, float Bottom)
{
    public float Width => Math.Max(0, Right - Left);
    public float Height => Math.Max(0, Bottom - Top);
    public float CenterX => (Left + Right) / 2F;
    public float CenterY => (Top + Bottom) / 2F;

    public static OcrBounds FromLTRB(float left, float top, float right, float bottom)
    {
        if (!float.IsFinite(left) || !float.IsFinite(top) ||
            !float.IsFinite(right) || !float.IsFinite(bottom))
        {
            throw new ArgumentOutOfRangeException(nameof(left), "OCR coordinates must be finite numbers.");
        }

        if (right < left || bottom < top)
        {
            throw new ArgumentException("OCR bounds must have non-negative width and height.");
        }

        return new OcrBounds(left, top, right, bottom);
    }
}

public sealed record OcrToken(
    string SourceImageId,
    string Text,
    OcrBounds Bounds,
    float Confidence)
{
    public string SourceImageId { get; init; } =
        string.IsNullOrWhiteSpace(SourceImageId)
            ? throw new ArgumentException("A source image id is required.", nameof(SourceImageId))
            : SourceImageId;

    public string Text { get; init; } = Text?.Trim() ?? string.Empty;

    public float Confidence { get; init; } =
        float.IsFinite(Confidence) ? Math.Clamp(Confidence, 0F, 1F) : 0F;
}

public sealed class GeometryOptions
{
    public float RowToleranceFactor { get; set; } = 0.72F;
    public float MinimumRowTolerance { get; set; } = 0.006F;
    public float ColumnToleranceFactor { get; set; } = 0.72F;
    public float MinimumColumnTolerance { get; set; } = 0.012F;
    public float LowConfidenceThreshold { get; set; } = 0.62F;
    public int MinimumPopulatedTargetCells { get; set; } = 1;
}

public sealed class TableLayoutProfile
{
    public int DateColumn { get; set; } = 1;
    public int WeekdayColumn { get; set; } = 2;
    public int Role1Column { get; set; } = 3;
    public int Role2Column { get; set; } = 4;
    public int Role3Column { get; set; } = -2;
    public int Role4Column { get; set; } = -1;
    public float BodyTop { get; set; } = 0F;
    public string CellTextSeparator { get; set; } = "、";
    public GeometryOptions Geometry { get; set; } = new();

    public IReadOnlyList<int> TargetColumns =>
        [DateColumn, WeekdayColumn, Role1Column, Role2Column, Role3Column, Role4Column];

    public void Validate()
    {
        if (TargetColumns.Any(static value => value == 0))
        {
            throw new ArgumentException("Column 0 is invalid. Use 1 for the first column or -1 for the last column.");
        }

        if (TargetColumns.Distinct().Count() != TargetColumns.Count)
        {
            throw new ArgumentException("The six target columns must be different.");
        }

        if (BodyTop is < 0F or >= 1F)
        {
            throw new ArgumentOutOfRangeException(nameof(BodyTop), "BodyTop must be within [0, 1).");
        }

        ArgumentNullException.ThrowIfNull(Geometry);
    }
}

public enum ScheduleIssueCode
{
    MissingWorkday,
    DuplicateDate,
    InvalidDate,
    InvalidWeekday,
    WeekdayMismatch,
    EmptyRole1,
    EmptyRole2,
    EmptyRole3,
    EmptyRole4,
    AmbiguousRow,
    AmbiguousColumn,
    LowOcrConfidence,
    MultipleMonths,
    OutOfTargetMonth,
    UnassignedOcrToken,
    InsufficientColumns,
}

public sealed record ScheduleIssue(
    ScheduleIssueCode Code,
    string Message,
    string? PropertyName = null,
    string? SourceImageId = null);

public sealed class ScheduleCandidate
{
    public string RawDateText { get; set; } = string.Empty;
    public string RawWeekdayText { get; set; } = string.Empty;
    public string Role1 { get; set; } = string.Empty;
    public string Role2 { get; set; } = string.Empty;
    public string Role3 { get; set; } = string.Empty;
    public string Role4 { get; set; } = string.Empty;
    public string SourceImageId { get; set; } = string.Empty;
    public float RowCenterY { get; set; }
    public List<ScheduleIssue> Issues { get; } = [];
    public List<OcrToken> SourceTokens { get; } = [];
}

public sealed class ScheduleRow
{
    public DateOnly? Date { get; set; }
    public string RawDateText { get; set; } = string.Empty;
    public string Weekday { get; set; } = string.Empty;
    public string Role1 { get; set; } = string.Empty;
    public string Role2 { get; set; } = string.Empty;
    public string Role3 { get; set; } = string.Empty;
    public string Role4 { get; set; } = string.Empty;
    public string SourceImageId { get; set; } = string.Empty;
    public bool IsMissingPlaceholder { get; set; }
    public List<ScheduleIssue> Issues { get; } = [];
}

public sealed class ScheduleExtractionResult
{
    public List<ScheduleCandidate> Candidates { get; } = [];
    public List<OcrToken> UnassignedTokens { get; } = [];
    public List<ScheduleIssue> Issues { get; } = [];
}

public sealed class ScheduleBuildResult
{
    public List<ScheduleRow> Rows { get; } = [];
    public List<ScheduleCandidate> UnlocatedCandidates { get; } = [];
    public List<ScheduleCandidate> WeekendCandidates { get; } = [];
    public List<ScheduleCandidate> DuplicateCandidates { get; } = [];
    public List<ScheduleIssue> Issues { get; } = [];

    public int ProblemRowCount => Rows.Count(static row => row.Issues.Count > 0);
    public bool HasProblems => ProblemRowCount > 0 || Issues.Count > 0 || UnlocatedCandidates.Count > 0;
}
