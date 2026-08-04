namespace RapidOCRSchedule.Core;

/// <summary>
/// Merges extracted rows by date, creates all missing calendar dates,
/// and performs the business validations required by the schedule workflow.
/// </summary>
public sealed class ScheduleService
{
    public ScheduleBuildResult Build(
        int targetYear,
        int targetMonth,
        IEnumerable<ScheduleCandidate> candidates,
        bool alignOcrRowsToCalendar = false,
        bool ignoreWeekends = false)
    {
        if (targetYear is < 1900 or > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(targetYear));
        }

        if (targetMonth is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(targetMonth));
        }

        ArgumentNullException.ThrowIfNull(candidates);
        List<ScheduleCandidate> preparedCandidates = candidates.ToList();
        if (alignOcrRowsToCalendar)
        {
            preparedCandidates = AlignOcrRowsToCalendar(targetYear, targetMonth, preparedCandidates);
        }

        ScheduleBuildResult result = new();
        List<LocatedCandidate> located = [];

        foreach (ScheduleCandidate candidate in preparedCandidates)
        {
            // Build may be rerun when the user changes the target month. Remove findings that
            // are relative to the previous target so they cannot leak into the new result.
            candidate.Issues.RemoveAll(static issue =>
                issue.Code is ScheduleIssueCode.InvalidDate or
                    ScheduleIssueCode.OutOfTargetMonth or
                    ScheduleIssueCode.InvalidWeekday or
                    ScheduleIssueCode.WeekdayMismatch or
                    ScheduleIssueCode.EmptyRole1 or
                    ScheduleIssueCode.EmptyRole2 or
                    ScheduleIssueCode.EmptyRole3 or
                    ScheduleIssueCode.EmptyRole4);

            if (!DateTextParser.TryCreateDate(targetYear, candidate.RawDateText, out DateOnly date) &&
                !TryRecoverCompactOcrDate(targetYear, targetMonth, candidate, out date))
            {
                AddIssueIfMissing(
                    candidate.Issues,
                    new ScheduleIssue(
                        ScheduleIssueCode.InvalidDate,
                        "日期无法识别，请手动填写有效的月/日。",
                        nameof(ScheduleRow.RawDateText),
                        candidate.SourceImageId));
                ValidateUnlocatedCandidate(candidate, date: null);
                result.UnlocatedCandidates.Add(candidate);
                continue;
            }

            candidate.RawDateText = date.ToString("M/d");
            located.Add(new LocatedCandidate(candidate, date));
        }

        int[] recognizedMonths = located.Select(static item => item.Date.Month).Distinct().Order().ToArray();
        if (recognizedMonths.Length > 1)
        {
            result.Issues.Add(new ScheduleIssue(
                ScheduleIssueCode.MultipleMonths,
                $"4 张图片中识别到多个月份：{string.Join("、", recognizedMonths.Select(static month => $"{month}月"))}。请核对目标月份和原图。"));
        }

        foreach (LocatedCandidate item in located.Where(item => item.Date.Month != targetMonth))
        {
            AddIssueIfMissing(
                item.Candidate.Issues,
                new ScheduleIssue(
                    ScheduleIssueCode.OutOfTargetMonth,
                    $"识别日期 {item.Date:M/d} 不属于目标月份 {targetMonth} 月。",
                    nameof(ScheduleRow.RawDateText),
                    item.Candidate.SourceImageId));
            ValidateUnlocatedCandidate(item.Candidate, item.Date);
            result.UnlocatedCandidates.Add(item.Candidate);
        }

        List<LocatedCandidate> targetMonthCandidates = located
            .Where(item => item.Date.Month == targetMonth)
            .ToList();
        if (ignoreWeekends)
        {
            result.WeekendCandidates.AddRange(
                targetMonthCandidates
                    .Where(static item => IsWeekend(item.Date))
                    .Select(static item => item.Candidate));
            targetMonthCandidates = targetMonthCandidates
                .Where(static item => !IsWeekend(item.Date))
                .ToList();
        }

        Dictionary<DateOnly, List<LocatedCandidate>> byDate = targetMonthCandidates
            .GroupBy(static item => item.Date)
            .ToDictionary(static group => group.Key, static group => group.ToList());

        DateOnly first = new(targetYear, targetMonth, 1);
        DateOnly last = new(targetYear, targetMonth, DateTime.DaysInMonth(targetYear, targetMonth));
        for (DateOnly date = first; date <= last; date = date.AddDays(1))
        {
            if (ignoreWeekends && IsWeekend(date))
            {
                continue;
            }

            if (!byDate.TryGetValue(date, out List<LocatedCandidate>? matches) || matches.Count == 0)
            {
                ScheduleRow missing = new()
                {
                    Date = date,
                    RawDateText = date.ToString("M/d"),
                    Weekday = WeekdayTextParser.ToChinese(date.DayOfWeek),
                    IsMissingPlaceholder = true,
                };
                missing.Issues.Add(new ScheduleIssue(
                    ScheduleIssueCode.MissingWorkday,
                    "该日期未在 4 张图片中找到，请手动补充。"));
                result.Rows.Add(missing);
                continue;
            }

            LocatedCandidate selected = matches
                .OrderBy(static item => item.Candidate.SourceImageId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static item => item.Candidate.RowCenterY)
                .First();
            ScheduleRow row = CreateRow(selected.Candidate, date);

            if (matches.Count > 1)
            {
                result.DuplicateCandidates.AddRange(matches.Select(static item => item.Candidate));
                string sources = string.Join(
                    "、",
                    matches.Select(static item => Path.GetFileName(item.Candidate.SourceImageId)).Distinct());
                row.SourceImageId = string.Join(
                    "; ",
                    matches.Select(static item => item.Candidate.SourceImageId).Distinct(StringComparer.OrdinalIgnoreCase));
                row.Issues.Add(new ScheduleIssue(
                    ScheduleIssueCode.DuplicateDate,
                    $"同一日期识别出 {matches.Count} 条记录（{sources}），当前显示第一条，请对照原图决定。"));
            }

            ValidateRow(row, selected.Candidate.RawWeekdayText);
            result.Rows.Add(row);
        }

        return result;
    }

    public ScheduleBuildResult Revalidate(
        int targetYear,
        int targetMonth,
        IEnumerable<ScheduleRow> editedRows,
        bool ignoreWeekends = false)
    {
        ArgumentNullException.ThrowIfNull(editedRows);
        return Build(
            targetYear,
            targetMonth,
            CreateCandidatesFromRows(editedRows),
            ignoreWeekends: ignoreWeekends);
    }

    /// <summary>
    /// Revalidates an untouched build result without losing duplicate or unlocated source rows.
    /// UI callers with edited rows should use the enumerable overload and include their editable
    /// unlocated rows, as the WinForms application does.
    /// </summary>
    public ScheduleBuildResult Revalidate(
        int targetYear,
        int targetMonth,
        ScheduleBuildResult previous,
        bool ignoreWeekends = false)
    {
        ArgumentNullException.ThrowIfNull(previous);
        List<ScheduleCandidate> candidates = CreateCandidatesFromRows(
            previous.Rows.Where(static row =>
                row.Issues.All(static issue => issue.Code != ScheduleIssueCode.DuplicateDate)));
        candidates.AddRange(previous.UnlocatedCandidates);
        candidates.AddRange(previous.DuplicateCandidates);
        candidates.AddRange(previous.WeekendCandidates);
        return Build(targetYear, targetMonth, candidates, ignoreWeekends: ignoreWeekends);
    }

    private static bool IsWeekend(DateOnly date) =>
        date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    private static List<ScheduleCandidate> CreateCandidatesFromRows(IEnumerable<ScheduleRow> editedRows)
    {
        List<ScheduleCandidate> candidates = [];
        foreach (ScheduleRow row in editedRows)
        {
            bool completelyEmpty = string.IsNullOrWhiteSpace(row.Role1) &&
                                   string.IsNullOrWhiteSpace(row.Role2) &&
                                   string.IsNullOrWhiteSpace(row.Role3) &&
                                   string.IsNullOrWhiteSpace(row.Role4);
            if (row.IsMissingPlaceholder && completelyEmpty)
            {
                continue;
            }

            ScheduleCandidate candidate = new()
            {
                RawDateText = !string.IsNullOrWhiteSpace(row.RawDateText)
                    ? row.RawDateText
                    : row.Date?.ToString("M/d") ?? string.Empty,
                RawWeekdayText = row.Weekday,
                Role1 = row.Role1,
                Role2 = row.Role2,
                Role3 = row.Role3,
                Role4 = row.Role4,
                SourceImageId = string.IsNullOrWhiteSpace(row.SourceImageId) ? "手动编辑" : row.SourceImageId,
            };
            candidate.Issues.AddRange(row.Issues.Where(static issue => IsPersistentExtractionIssue(issue.Code)));
            candidates.Add(candidate);
        }

        return candidates;
    }

    private static ScheduleRow CreateRow(ScheduleCandidate candidate, DateOnly date)
    {
        ScheduleRow row = new()
        {
            Date = date,
            RawDateText = date.ToString("M/d"),
            Weekday = WeekdayTextParser.ToChinese(date.DayOfWeek),
            Role1 = candidate.Role1.Trim(),
            Role2 = candidate.Role2.Trim(),
            Role3 = candidate.Role3.Trim(),
            Role4 = candidate.Role4.Trim(),
            SourceImageId = candidate.SourceImageId,
        };
        row.Issues.AddRange(candidate.Issues);
        return row;
    }

    private static void ValidateRow(ScheduleRow row, string rawWeekdayText)
    {
        if (row.Date is not DateOnly date)
        {
            row.Issues.Add(new ScheduleIssue(
                ScheduleIssueCode.InvalidDate,
                "日期无法识别。",
                nameof(ScheduleRow.RawDateText)));
            return;
        }

        if (!WeekdayTextParser.TryParse(rawWeekdayText, out DayOfWeek recognizedWeekday))
        {
            row.Issues.Add(new ScheduleIssue(
                ScheduleIssueCode.InvalidWeekday,
                "星期无法识别，已按日期自动计算。",
                nameof(ScheduleRow.Weekday)));
        }
        else if (recognizedWeekday != date.DayOfWeek)
        {
            row.Issues.Add(new ScheduleIssue(
                ScheduleIssueCode.WeekdayMismatch,
                $"OCR 星期为“{rawWeekdayText}”，与日期对应的{WeekdayTextParser.ToChinese(date.DayOfWeek)}不一致。",
                nameof(ScheduleRow.Weekday)));
        }

        AddEmptyRoleIssue(row, row.Role1, ScheduleIssueCode.EmptyRole1, nameof(ScheduleRow.Role1), "岗位一（队长）为空。");
        AddEmptyRoleIssue(row, row.Role2, ScheduleIssueCode.EmptyRole2, nameof(ScheduleRow.Role2), "岗位二为空。");
        AddEmptyRoleIssue(row, row.Role3, ScheduleIssueCode.EmptyRole3, nameof(ScheduleRow.Role3), "岗位三为空。");
        AddEmptyRoleIssue(row, row.Role4, ScheduleIssueCode.EmptyRole4, nameof(ScheduleRow.Role4), "岗位四为空。");
    }

    private static void AddEmptyRoleIssue(
        ScheduleRow row,
        string value,
        ScheduleIssueCode code,
        string propertyName,
        string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            row.Issues.Add(new ScheduleIssue(code, message, propertyName, row.SourceImageId));
        }
    }

    private static void AddIssueIfMissing(List<ScheduleIssue> issues, ScheduleIssue issue)
    {
        if (issues.All(existing => existing.Code != issue.Code))
        {
            issues.Add(issue);
        }
    }

    private static void ValidateUnlocatedCandidate(ScheduleCandidate candidate, DateOnly? date)
    {
        if (!WeekdayTextParser.TryParse(candidate.RawWeekdayText, out DayOfWeek weekday))
        {
            AddIssueIfMissing(
                candidate.Issues,
                new ScheduleIssue(
                    ScheduleIssueCode.InvalidWeekday,
                    "星期无法识别。",
                    nameof(ScheduleRow.Weekday),
                    candidate.SourceImageId));
        }
        else if (date.HasValue && weekday != date.Value.DayOfWeek)
        {
            AddIssueIfMissing(
                candidate.Issues,
                new ScheduleIssue(
                    ScheduleIssueCode.WeekdayMismatch,
                    $"OCR 星期为“{candidate.RawWeekdayText}”，与日期对应的{WeekdayTextParser.ToChinese(date.Value.DayOfWeek)}不一致。",
                    nameof(ScheduleRow.Weekday),
                    candidate.SourceImageId));
        }

        AddCandidateEmptyRoleIssue(candidate, candidate.Role1, ScheduleIssueCode.EmptyRole1, nameof(ScheduleRow.Role1), "岗位一（队长）为空。");
        AddCandidateEmptyRoleIssue(candidate, candidate.Role2, ScheduleIssueCode.EmptyRole2, nameof(ScheduleRow.Role2), "岗位二为空。");
        AddCandidateEmptyRoleIssue(candidate, candidate.Role3, ScheduleIssueCode.EmptyRole3, nameof(ScheduleRow.Role3), "岗位三为空。");
        AddCandidateEmptyRoleIssue(candidate, candidate.Role4, ScheduleIssueCode.EmptyRole4, nameof(ScheduleRow.Role4), "岗位四为空。");
    }

    private static void AddCandidateEmptyRoleIssue(
        ScheduleCandidate candidate,
        string value,
        ScheduleIssueCode code,
        string propertyName,
        string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            AddIssueIfMissing(
                candidate.Issues,
                new ScheduleIssue(code, message, propertyName, candidate.SourceImageId));
        }
    }

    private static List<ScheduleCandidate> AlignOcrRowsToCalendar(
        int targetYear,
        int targetMonth,
        List<ScheduleCandidate> candidates)
    {
        ImageSeries[] series = candidates
            .GroupBy(static candidate => candidate.SourceImageId, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static group => new ImageSeries(
                group.Key,
                group.OrderBy(static candidate => candidate.RowCenterY).ToList()))
            .ToArray();
        if (series.Length != 4 || series.Any(static value => string.IsNullOrWhiteSpace(value.SourceImageId)))
        {
            return candidates;
        }

        int daysInMonth = DateTime.DaysInMonth(targetYear, targetMonth);
        List<CalendarAssignment> assignments = [];
        foreach (int[] offsets in GetFourDaySeriesPermutations())
        {
            bool countsMatch = true;
            int score = 0;
            for (int index = 0; index < series.Length; index++)
            {
                int expectedCount = ((daysInMonth - offsets[index]) / 4) + 1;
                if (series[index].Candidates.Count != expectedCount)
                {
                    countsMatch = false;
                    break;
                }

                score += ScoreSeries(
                    targetYear,
                    targetMonth,
                    offsets[index],
                    series[index].Candidates);
            }

            if (countsMatch)
            {
                assignments.Add(new CalendarAssignment(offsets, score));
            }
        }

        CalendarAssignment[] ranked = assignments
            .OrderByDescending(static assignment => assignment.Score)
            .ToArray();
        if (ranked.Length == 0 || ranked[0].Score <= 0 ||
            (ranked.Length > 1 && ranked[0].Score == ranked[1].Score))
        {
            return candidates;
        }

        Dictionary<ScheduleCandidate, DateOnly> calendarDates = [];
        for (int seriesIndex = 0; seriesIndex < series.Length; seriesIndex++)
        {
            int firstDay = ranked[0].Offsets[seriesIndex];
            for (int rowIndex = 0; rowIndex < series[seriesIndex].Candidates.Count; rowIndex++)
            {
                calendarDates[series[seriesIndex].Candidates[rowIndex]] =
                    new DateOnly(targetYear, targetMonth, firstDay + rowIndex * 4);
            }
        }

        return candidates.Select(candidate =>
        {
            if (!calendarDates.TryGetValue(candidate, out DateOnly calendarDate))
            {
                return candidate;
            }

            ScheduleCandidate corrected = CloneCandidate(candidate);
            corrected.RawDateText = calendarDate.ToString("M/d");
            corrected.RawWeekdayText = WeekdayTextParser.ToChinese(calendarDate.DayOfWeek);
            return corrected;
        }).ToList();
    }

    private static int ScoreSeries(
        int targetYear,
        int targetMonth,
        int firstDay,
        IReadOnlyList<ScheduleCandidate> candidates)
    {
        int score = 0;
        for (int index = 0; index < candidates.Count; index++)
        {
            DateOnly expected = new(targetYear, targetMonth, firstDay + index * 4);
            ScheduleCandidate candidate = candidates[index];
            if (DateTextParser.TryCreateDate(targetYear, candidate.RawDateText, out DateOnly recognizedDate))
            {
                if (recognizedDate.Month != targetMonth)
                {
                    score -= 20;
                }
                else if (recognizedDate.Day == expected.Day)
                {
                    score += 30;
                }
                else
                {
                    score -= Math.Min(30, Math.Abs(recognizedDate.Day - expected.Day) * 5);
                }
            }

            if (WeekdayTextParser.TryParse(candidate.RawWeekdayText, out DayOfWeek recognizedWeekday))
            {
                score += recognizedWeekday == expected.DayOfWeek ? 10 : -10;
            }
        }

        return score;
    }

    private static IEnumerable<int[]> GetFourDaySeriesPermutations()
    {
        for (int first = 1; first <= 4; first++)
        {
            for (int second = 1; second <= 4; second++)
            {
                for (int third = 1; third <= 4; third++)
                {
                    for (int fourth = 1; fourth <= 4; fourth++)
                    {
                        int[] values = [first, second, third, fourth];
                        if (values.Distinct().Count() == 4)
                        {
                            yield return values;
                        }
                    }
                }
            }
        }
    }

    private static ScheduleCandidate CloneCandidate(ScheduleCandidate source)
    {
        ScheduleCandidate clone = new()
        {
            RawDateText = source.RawDateText,
            RawWeekdayText = source.RawWeekdayText,
            Role1 = source.Role1,
            Role2 = source.Role2,
            Role3 = source.Role3,
            Role4 = source.Role4,
            SourceImageId = source.SourceImageId,
            RowCenterY = source.RowCenterY,
        };
        clone.Issues.AddRange(source.Issues);
        clone.SourceTokens.AddRange(source.SourceTokens);
        return clone;
    }

    private static bool TryRecoverCompactOcrDate(
        int targetYear,
        int targetMonth,
        ScheduleCandidate candidate,
        out DateOnly date)
    {
        date = default;
        string compact = DateTextParser.Normalize(candidate.RawDateText)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .TrimEnd('日', '号');
        string monthText = targetMonth.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!compact.StartsWith(monthText, StringComparison.Ordinal) || compact.Length <= monthText.Length)
        {
            return false;
        }

        List<DateOnly> alternatives = [];
        AddCompactDateAlternative(compact[monthText.Length..], targetYear, targetMonth, alternatives);

        char possibleSeparator = compact[monthText.Length];
        if (possibleSeparator is '1' or 'I' or 'i' or 'l' or '|' or '!')
        {
            AddCompactDateAlternative(compact[(monthText.Length + 1)..], targetYear, targetMonth, alternatives);
        }

        alternatives = alternatives.Distinct().ToList();
        if (alternatives.Count == 1)
        {
            date = alternatives[0];
            return true;
        }

        if (alternatives.Count > 1 &&
            WeekdayTextParser.TryParse(candidate.RawWeekdayText, out DayOfWeek weekday))
        {
            DateOnly[] weekdayMatches = alternatives
                .Where(value => value.DayOfWeek == weekday)
                .ToArray();
            if (weekdayMatches.Length == 1)
            {
                date = weekdayMatches[0];
                return true;
            }
        }

        return false;
    }

    private static void AddCompactDateAlternative(
        string dayText,
        int targetYear,
        int targetMonth,
        List<DateOnly> alternatives)
    {
        if (!int.TryParse(
                dayText,
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out int day))
        {
            return;
        }

        try
        {
            alternatives.Add(new DateOnly(targetYear, targetMonth, day));
        }
        catch (ArgumentOutOfRangeException)
        {
            // This interpretation is not a real calendar date.
        }
    }

    private static bool IsPersistentExtractionIssue(ScheduleIssueCode code) => code is
        ScheduleIssueCode.DuplicateDate or
        ScheduleIssueCode.AmbiguousRow or
        ScheduleIssueCode.AmbiguousColumn or
        ScheduleIssueCode.LowOcrConfidence or
        ScheduleIssueCode.InsufficientColumns;

    private sealed record ImageSeries(string SourceImageId, List<ScheduleCandidate> Candidates);
    private sealed record CalendarAssignment(int[] Offsets, int Score);
    private sealed record LocatedCandidate(ScheduleCandidate Candidate, DateOnly Date);
}
