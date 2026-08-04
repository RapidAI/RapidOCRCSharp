using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace RapidOCRSchedule.Core;

public sealed class EmployeeNameResolver
{
    private const uint SimplifiedChineseMapFlag = 0x02000000;
    private readonly IReadOnlyList<EmployeeNameEntry> _entries;
    private readonly Dictionary<string, string> _exactNames;

    public EmployeeNameResolver(IEnumerable<string>? employeeNames)
    {
        IReadOnlyList<string> normalizedNames = NormalizeEmployeeNames(employeeNames);
        _entries = normalizedNames
            .Select(static name => new EmployeeNameEntry(name, Canonicalize(name)))
            .ToArray();
        _exactNames = _entries.ToDictionary(static entry => entry.CanonicalName, static entry => entry.Name, StringComparer.Ordinal);
    }

    public static IReadOnlyList<string> NormalizeEmployeeNames(IEnumerable<string>? employeeNames)
    {
        if (employeeNames is null)
        {
            return [];
        }

        List<string> normalizedNames = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (string? employeeName in employeeNames)
        {
            string normalized = ToSimplifiedChinese(employeeName).Trim();
            string canonical = Canonicalize(normalized);
            if (canonical.Length == 0 || !seen.Add(canonical))
            {
                continue;
            }

            normalizedNames.Add(normalized);
        }

        return normalizedNames;
    }

    public string Resolve(string? recognizedName)
    {
        string simplified = ToSimplifiedChinese(recognizedName).Trim();
        string canonical = Canonicalize(simplified);
        if (canonical.Length == 0)
        {
            return string.Empty;
        }

        if (_exactNames.TryGetValue(canonical, out string? exactName))
        {
            return exactName;
        }

        // Fuzzy correction is deliberately conservative. Chinese employee names with
        // fewer than three characters are too easy to confuse, and matching surnames
        // must agree. Ambiguous one-edit matches are left unchanged for manual review.
        if (canonical.Length < 3)
        {
            return simplified;
        }

        string? uniqueMatch = null;
        foreach (EmployeeNameEntry entry in _entries)
        {
            if (entry.CanonicalName.Length < 3 ||
                entry.CanonicalName[0] != canonical[0] ||
                Math.Abs(entry.CanonicalName.Length - canonical.Length) > 1 ||
                EditDistanceAtMostOne(canonical, entry.CanonicalName) > 1)
            {
                continue;
            }

            if (uniqueMatch is not null)
            {
                return simplified;
            }

            uniqueMatch = entry.Name;
        }

        return uniqueMatch ?? simplified;
    }

    public static string ToSimplifiedChinese(string? value)
    {
        string normalized = (value ?? string.Empty).Normalize(NormalizationForm.FormKC);
        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        if (!OperatingSystem.IsWindows())
        {
            return normalized;
        }

        try
        {
            return MapToSimplifiedChinese(normalized);
        }
        catch (InvalidOperationException)
        {
            // Name-list matching still provides a safe one-edit fallback if Windows
            // cannot perform the simplified-Chinese conversion on this installation.
            return normalized;
        }
    }

    [SupportedOSPlatform("windows")]
    private static string MapToSimplifiedChinese(string value)
    {
        int requiredLength = LCMapStringEx(
            "zh-CN",
            SimplifiedChineseMapFlag,
            value,
            -1,
            null,
            0,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);
        if (requiredLength == 0)
        {
            throw new InvalidOperationException($"Windows 中文转换失败，错误码：{Marshal.GetLastWin32Error()}。");
        }

        StringBuilder destination = new(requiredLength);
        int writtenLength = LCMapStringEx(
            "zh-CN",
            SimplifiedChineseMapFlag,
            value,
            -1,
            destination,
            destination.Capacity,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);
        if (writtenLength == 0)
        {
            throw new InvalidOperationException($"Windows 中文转换失败，错误码：{Marshal.GetLastWin32Error()}。");
        }

        return destination.ToString();
    }

    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [SupportedOSPlatform("windows")]
    private static extern int LCMapStringEx(
        string localeName,
        uint mapFlags,
        string source,
        int sourceLength,
        StringBuilder? destination,
        int destinationLength,
        IntPtr versionInformation,
        IntPtr reserved,
        IntPtr sortHandle);

    private static string Canonicalize(string value)
    {
        StringBuilder builder = new(value.Length);
        foreach (char character in value.Normalize(NormalizationForm.FormKC))
        {
            if (!char.IsWhiteSpace(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static int EditDistanceAtMostOne(string left, string right)
    {
        if (left.Equals(right, StringComparison.Ordinal))
        {
            return 0;
        }

        if (Math.Abs(left.Length - right.Length) > 1)
        {
            return 2;
        }

        int leftIndex = 0;
        int rightIndex = 0;
        int edits = 0;
        while (leftIndex < left.Length && rightIndex < right.Length)
        {
            if (left[leftIndex] == right[rightIndex])
            {
                leftIndex++;
                rightIndex++;
                continue;
            }

            if (++edits > 1)
            {
                return edits;
            }

            if (left.Length > right.Length)
            {
                leftIndex++;
            }
            else if (right.Length > left.Length)
            {
                rightIndex++;
            }
            else
            {
                leftIndex++;
                rightIndex++;
            }
        }

        if (leftIndex < left.Length || rightIndex < right.Length)
        {
            edits++;
        }

        return edits;
    }

    private sealed record EmployeeNameEntry(string Name, string CanonicalName);
}
