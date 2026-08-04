using RapidOCRSchedule.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RapidOCRWinform;

internal sealed class ImageWorkItem
{
    public required string FilePath { get; init; }

    public string DisplayName => Path.GetFileName(FilePath);

    public string Status { get; set; } = "待识别";

    public string Diagnostics { get; set; } = string.Empty;

    public List<OcrToken> Tokens { get; } = [];
}

internal sealed class EditableScheduleRow : INotifyPropertyChanged
{
    private string _dateText = string.Empty;
    private string _weekday = string.Empty;
    private string _role1 = string.Empty;
    private string _role2 = string.Empty;
    private string _role3 = string.Empty;
    private string _role4 = string.Empty;
    private string _issueText = string.Empty;

    public string DateText
    {
        get => _dateText;
        set => SetField(ref _dateText, value?.Trim() ?? string.Empty);
    }

    public string Weekday
    {
        get => _weekday;
        set => SetField(ref _weekday, value?.Trim() ?? string.Empty);
    }

    public string Role1
    {
        get => _role1;
        set => SetField(ref _role1, value?.Trim() ?? string.Empty);
    }

    public string Role2
    {
        get => _role2;
        set => SetField(ref _role2, value?.Trim() ?? string.Empty);
    }

    public string Role3
    {
        get => _role3;
        set => SetField(ref _role3, value?.Trim() ?? string.Empty);
    }

    public string Role4
    {
        get => _role4;
        set => SetField(ref _role4, value?.Trim() ?? string.Empty);
    }

    public string IssueText
    {
        get => _issueText;
        set => SetField(ref _issueText, value ?? string.Empty);
    }

    public string SourceText { get; set; } = string.Empty;

    public bool IsMissingPlaceholder { get; set; }

    public bool IsUnlocatedCandidate { get; set; }

    public HashSet<string> InvalidProperties { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<ScheduleIssue> PersistentIssues { get; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField(ref string field, string value, [CallerMemberName] string? propertyName = null)
    {
        if (string.Equals(field, value, StringComparison.Ordinal))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
