using RapidOCRSchedule.Core;

namespace RapidOCRSchedule.Tests;

public sealed class CoreScheduleTests
{
    private const int TargetYear = 2026;
    private const int TargetMonth = 8;

    [Theory]
    [InlineData("8/6")]
    [InlineData("8-6")]
    [InlineData("8.6")]
    [InlineData("8月6日")]
    [InlineData("８／６")]
    [InlineData("８－６")]
    [InlineData("８月６日")]
    public void DateParser_AcceptsChineseAndFullWidthMonthDay(string text)
    {
        Assert.True(DateTextParser.TryParseMonthDay(text, out int month, out int day));
        Assert.Equal(8, month);
        Assert.Equal(6, day);

        Assert.True(DateTextParser.TryCreateDate(TargetYear, text, out DateOnly date));
        Assert.Equal(new DateOnly(TargetYear, 8, 6), date);
    }

    [Theory]
    [InlineData(2026, "")]
    [InlineData(2026, "8/32")]
    [InlineData(2026, "13月1日")]
    [InlineData(2026, "2月30日")]
    [InlineData(2025, "2月29日")]
    public void DateParser_RejectsInvalidDates(int year, string text)
    {
        Assert.False(DateTextParser.TryCreateDate(year, text, out _));
    }

    [Theory]
    [InlineData("星期一", DayOfWeek.Monday)]
    [InlineData(" 周一 ", DayOfWeek.Monday)]
    [InlineData("礼拜五", DayOfWeek.Friday)]
    [InlineData("星期天", DayOfWeek.Sunday)]
    [InlineData("周日", DayOfWeek.Sunday)]
    public void WeekdayParser_AcceptsCommonChineseForms(string text, DayOfWeek expected)
    {
        Assert.True(WeekdayTextParser.TryParse(text, out DayOfWeek actual));
        Assert.Equal(expected, actual);
        Assert.StartsWith("星期", WeekdayTextParser.ToChinese(actual), StringComparison.Ordinal);
    }

    [Fact]
    public void WeekdayParser_RejectsUnknownWeekday()
    {
        Assert.False(WeekdayTextParser.TryParse("礼拜八", out _));
    }

    [Fact]
    public void Build_FourFourDaySeries_IsOrderIndependentSortedAndComplete()
    {
        ScheduleCandidate[] ordered = CreateFourDaySeries().ToArray();
        ScheduleCandidate[] shuffled = ordered
            .OrderBy(static candidate => StableShuffleKey(candidate.RawDateText))
            .ToArray();
        ScheduleService service = new();

        ScheduleBuildResult first = service.Build(TargetYear, TargetMonth, ordered);
        ScheduleBuildResult second = service.Build(TargetYear, TargetMonth, shuffled);

        DateOnly[] expectedDates = DatesInTargetMonth().ToArray();
        Assert.Equal(expectedDates, first.Rows.Select(static row => row.Date!.Value));
        Assert.Equal(expectedDates, second.Rows.Select(static row => row.Date!.Value));
        Assert.Equal(
            first.Rows.Select(static row => (row.Date, row.Role1, row.Role2, row.Role3, row.Role4)),
            second.Rows.Select(static row => (row.Date, row.Role1, row.Role2, row.Role3, row.Role4)));
        Assert.All(first.Rows, static row => Assert.False(row.IsMissingPlaceholder));
        Assert.Empty(first.UnlocatedCandidates);
    }

    [Fact]
    public void Build_WhenWeekendIgnoringIsDisabled_IncludesWeekendCandidatesInOutput()
    {
        ScheduleBuildResult result = new ScheduleService().Build(
            TargetYear,
            TargetMonth,
            CreateFourDaySeries(),
            ignoreWeekends: false);

        Assert.Contains(
            result.Rows,
            static row => row.Date!.Value.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
        Assert.Equal(31, result.Rows.Count);
        Assert.Empty(result.WeekendCandidates);
    }

    [Fact]
    public void Build_WhenWeekendIgnoringIsEnabled_ExcludesWeekendRowsAndCapturesCandidates()
    {
        ScheduleBuildResult result = new ScheduleService().Build(
            TargetYear,
            TargetMonth,
            CreateFourDaySeries(),
            ignoreWeekends: true);

        Assert.DoesNotContain(
            result.Rows,
            static row => row.Date!.Value.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
        Assert.Equal(21, result.Rows.Count);
        Assert.Equal(10, result.WeekendCandidates.Count);
    }

    [Fact]
    public void Build_CreatesPlaceholderForEveryMissingWorkday()
    {
        ScheduleCandidate[] candidates = CreateFourDaySeries()
            .Where(static candidate => candidate.RawDateText != "8/5")
            .ToArray();

        ScheduleBuildResult result = new ScheduleService().Build(TargetYear, TargetMonth, candidates);
        ScheduleRow missing = Assert.Single(
            result.Rows,
            static row => row.Date == new DateOnly(TargetYear, TargetMonth, 5));

        Assert.True(missing.IsMissingPlaceholder);
        Assert.Contains(missing.Issues, static issue => issue.Code == ScheduleIssueCode.MissingWorkday);
    }

    [Fact]
    public void Build_RecoversSlashReadAsOneUsingRecognizedWeekday()
    {
        ScheduleCandidate candidate = CreateCandidate(7, "3.png", role1: "陆俊杰");
        candidate.RawDateText = "817";

        ScheduleBuildResult result = new ScheduleService().Build(TargetYear, TargetMonth, [candidate]);
        ScheduleRow row = Assert.Single(
            result.Rows,
            static value => value.Date == new DateOnly(TargetYear, TargetMonth, 7));

        Assert.Equal("8/7", candidate.RawDateText);
        Assert.Equal("陆俊杰", row.Role1);
        Assert.False(row.IsMissingPlaceholder);
        Assert.Empty(result.UnlocatedCandidates);
    }

    [Fact]
    public void Build_DoesNotGuessAmbiguousCompactDateWithoutRecognizedWeekday()
    {
        ScheduleCandidate candidate = CreateCandidate(7);
        candidate.RawDateText = "817";
        candidate.RawWeekdayText = "星期";

        ScheduleBuildResult result = new ScheduleService().Build(TargetYear, TargetMonth, [candidate]);

        Assert.Single(result.UnlocatedCandidates);
        Assert.Equal("817", candidate.RawDateText);
    }

    [Fact]
    public void Build_RecoversCompactDateWhenOnlyOneInterpretationIsValid()
    {
        ScheduleCandidate candidate = CreateCandidate(3);
        candidate.RawDateText = "83";
        candidate.RawWeekdayText = "星期";

        ScheduleBuildResult result = new ScheduleService().Build(TargetYear, TargetMonth, [candidate]);
        ScheduleRow row = Assert.Single(
            result.Rows,
            static value => value.Date == new DateOnly(TargetYear, TargetMonth, 3));

        Assert.Equal("8/3", candidate.RawDateText);
        Assert.False(row.IsMissingPlaceholder);
        Assert.Empty(result.UnlocatedCandidates);
    }

    [Fact]
    public void Build_OcrCalendarAlignment_CorrectsRecognizedDatesAndWeekdaysFromTargetMonth()
    {
        List<ScheduleCandidate> candidates = CreateFourDaySeries().ToList();
        ScheduleCandidate augustThird = Assert.Single(
            candidates,
            static candidate => candidate.RawDateText == "8/3");
        augustThird.RawDateText = "1";
        augustThird.RawWeekdayText = "8/2";

        ScheduleCandidate augustSeventh = Assert.Single(
            candidates,
            static candidate => candidate.RawDateText == "8/7");
        augustSeventh.RawDateText = "817";
        augustSeventh.RawWeekdayText = "星期";
        augustSeventh.Role1 = "陆俊杰";

        ScheduleBuildResult result = new ScheduleService().Build(
            TargetYear,
            TargetMonth,
            candidates.OrderBy(static candidate => StableShuffleKey($"{candidate.SourceImageId}:{candidate.RowCenterY}")),
            alignOcrRowsToCalendar: true);

        Assert.Equal(31, result.Rows.Count);
        Assert.Empty(result.UnlocatedCandidates);
        Assert.DoesNotContain(result.Rows, static row => row.IsMissingPlaceholder);
        Assert.All(
            result.Rows,
            static row => Assert.Equal(
                WeekdayTextParser.ToChinese(row.Date!.Value.DayOfWeek),
                row.Weekday));

        ScheduleRow correctedThird = Assert.Single(
            result.Rows,
            static row => row.Date == new DateOnly(TargetYear, TargetMonth, 3));
        ScheduleRow correctedSeventh = Assert.Single(
            result.Rows,
            static row => row.Date == new DateOnly(TargetYear, TargetMonth, 7));
        Assert.Equal("星期一", correctedThird.Weekday);
        Assert.Equal("星期五", correctedSeventh.Weekday);
        Assert.Equal("陆俊杰", correctedSeventh.Role1);
        Assert.DoesNotContain(
            correctedThird.Issues.Concat(correctedSeventh.Issues),
            static issue => issue.Code is ScheduleIssueCode.InvalidDate or
                ScheduleIssueCode.InvalidWeekday or
                ScheduleIssueCode.WeekdayMismatch);

        // Raw OCR diagnostics remain unchanged; only the final schedule is corrected.
        Assert.Equal("1", augustThird.RawDateText);
        Assert.Equal("817", augustSeventh.RawDateText);
    }

    [Fact]
    public void Build_OcrCalendarAlignment_RecomputesWeekdaysWhenTargetYearChanges()
    {
        const int changedYear = 2027;
        ScheduleBuildResult result = new ScheduleService().Build(
            changedYear,
            TargetMonth,
            CreateFourDaySeries(),
            alignOcrRowsToCalendar: true);

        ScheduleRow augustThird = Assert.Single(
            result.Rows,
            static row => row.Date == new DateOnly(changedYear, TargetMonth, 3));
        Assert.Equal("星期二", augustThird.Weekday);
        Assert.DoesNotContain(result.Rows, static row => row.IsMissingPlaceholder);
    }

    [Fact]
    public void Build_OcrCalendarAlignment_DoesNotGuessWhenImageSeriesAreAmbiguous()
    {
        ScheduleCandidate[] candidates = CreateFourDaySeries().ToArray();
        foreach (ScheduleCandidate candidate in candidates)
        {
            candidate.RawDateText = string.Empty;
            candidate.RawWeekdayText = string.Empty;
        }

        ScheduleBuildResult result = new ScheduleService().Build(
            TargetYear,
            TargetMonth,
            candidates,
            alignOcrRowsToCalendar: true);

        Assert.Equal(candidates.Length, result.UnlocatedCandidates.Count);
        Assert.All(result.Rows, static row => Assert.True(row.IsMissingPlaceholder));
    }

    [Fact]
    public void Build_FlagsDuplicateDateAndKeepsOneOutputRow()
    {
        List<ScheduleCandidate> candidates = CreateFourDaySeries().ToList();
        candidates.Add(CreateCandidate(3, sourceImageId: "duplicate.png", role1: "另一位队长"));

        ScheduleBuildResult result = new ScheduleService().Build(TargetYear, TargetMonth, candidates);
        ScheduleRow row = Assert.Single(
            result.Rows,
            static value => value.Date == new DateOnly(TargetYear, TargetMonth, 3));

        Assert.Contains(row.Issues, static issue => issue.Code == ScheduleIssueCode.DuplicateDate);
        Assert.Contains("duplicate.png", row.SourceImageId, StringComparison.Ordinal);
        Assert.Equal(2, result.DuplicateCandidates.Count);
    }

    [Fact]
    public void Build_FlagsMultipleMonthsAndKeepsOutOfMonthCandidateForReview()
    {
        ScheduleCandidate august = CreateCandidate(3);
        ScheduleCandidate september = CreateCandidate(1);
        september.RawDateText = "9/1";

        ScheduleBuildResult result = new ScheduleService().Build(
            TargetYear,
            TargetMonth,
            [august, september]);

        Assert.Contains(result.Issues, static issue => issue.Code == ScheduleIssueCode.MultipleMonths);
        Assert.Contains(september, result.UnlocatedCandidates);
        Assert.Contains(september.Issues, static issue => issue.Code == ScheduleIssueCode.OutOfTargetMonth);
    }

    [Fact]
    public void Build_WhenTargetMonthChanges_DoesNotKeepStaleOutOfMonthIssue()
    {
        ScheduleCandidate september = CreateCandidate(1);
        september.RawDateText = "9/1";
        september.RawWeekdayText = WeekdayTextParser.ToChinese(new DateOnly(TargetYear, 9, 1).DayOfWeek);
        ScheduleService service = new();

        service.Build(TargetYear, 8, [september]);
        Assert.Contains(september.Issues, static issue => issue.Code == ScheduleIssueCode.OutOfTargetMonth);

        ScheduleBuildResult septemberResult = service.Build(TargetYear, 9, [september]);
        ScheduleRow row = Assert.Single(
            septemberResult.Rows,
            static value => value.Date == new DateOnly(TargetYear, 9, 1));
        Assert.DoesNotContain(row.Issues, static issue => issue.Code == ScheduleIssueCode.OutOfTargetMonth);
        Assert.DoesNotContain(september.Issues, static issue => issue.Code == ScheduleIssueCode.OutOfTargetMonth);
    }

    [Fact]
    public void Build_FlagsWeekdayMismatch()
    {
        ScheduleCandidate candidate = CreateCandidate(3);
        candidate.RawWeekdayText = "星期二"; // 2026-08-03 is Monday.

        ScheduleBuildResult result = new ScheduleService().Build(TargetYear, TargetMonth, [candidate]);
        ScheduleRow row = Assert.Single(
            result.Rows,
            static value => value.Date == new DateOnly(TargetYear, TargetMonth, 3));

        Assert.Contains(row.Issues, static issue => issue.Code == ScheduleIssueCode.WeekdayMismatch);
    }

    [Fact]
    public void Build_FlagsEachEmptyRole()
    {
        ScheduleCandidate candidate = CreateCandidate(3);
        candidate.Role1 = "";
        candidate.Role2 = " ";
        candidate.Role3 = "";
        candidate.Role4 = "";

        ScheduleBuildResult result = new ScheduleService().Build(TargetYear, TargetMonth, [candidate]);
        ScheduleRow row = Assert.Single(
            result.Rows,
            static value => value.Date == new DateOnly(TargetYear, TargetMonth, 3));

        ScheduleIssueCode[] expected =
        [
            ScheduleIssueCode.EmptyRole1,
            ScheduleIssueCode.EmptyRole2,
            ScheduleIssueCode.EmptyRole3,
            ScheduleIssueCode.EmptyRole4,
        ];
        Assert.All(expected, code => Assert.Contains(row.Issues, issue => issue.Code == code));
    }

    [Fact]
    public void Build_InvalidDateCandidate_AlsoReportsInvalidWeekdayAndEmptyRoles()
    {
        ScheduleCandidate candidate = new()
        {
            RawDateText = "8/32",
            RawWeekdayText = "不清楚",
            SourceImageId = "damaged.png",
        };

        ScheduleBuildResult result = new ScheduleService().Build(TargetYear, TargetMonth, [candidate]);
        ScheduleCandidate unlocated = Assert.Single(result.UnlocatedCandidates);
        ScheduleIssueCode[] expected =
        [
            ScheduleIssueCode.InvalidDate,
            ScheduleIssueCode.InvalidWeekday,
            ScheduleIssueCode.EmptyRole1,
            ScheduleIssueCode.EmptyRole2,
            ScheduleIssueCode.EmptyRole3,
            ScheduleIssueCode.EmptyRole4,
        ];

        Assert.All(expected, code => Assert.Contains(unlocated.Issues, issue => issue.Code == code));
    }

    [Fact]
    public void Revalidate_PreservesPersistentOcrAndDuplicateFindings()
    {
        ScheduleCandidate first = CreateCandidate(3, "first.png");
        first.Issues.Add(new ScheduleIssue(ScheduleIssueCode.LowOcrConfidence, "低置信度"));
        ScheduleCandidate duplicate = CreateCandidate(3, "second.png");
        ScheduleService service = new();

        ScheduleBuildResult initial = service.Build(TargetYear, TargetMonth, [first, duplicate]);
        ScheduleBuildResult revalidated = service.Revalidate(TargetYear, TargetMonth, initial.Rows);
        ScheduleRow row = Assert.Single(
            revalidated.Rows,
            static value => value.Date == new DateOnly(TargetYear, TargetMonth, 3));

        Assert.Contains(row.Issues, static issue => issue.Code == ScheduleIssueCode.LowOcrConfidence);
        Assert.Contains(row.Issues, static issue => issue.Code == ScheduleIssueCode.DuplicateDate);
    }

    [Fact]
    public void Revalidate_WhenUnlocatedRowIsIncluded_KeepsCrossMonthFinding()
    {
        ScheduleRow crossMonth = new()
        {
            RawDateText = "9/1",
            Weekday = "星期二",
            Role1 = "甲",
            Role2 = "乙",
            Role3 = "丙",
            Role4 = "丁",
            SourceImageId = "september.png",
        };

        ScheduleBuildResult result = new ScheduleService().Revalidate(
            TargetYear,
            TargetMonth,
            [crossMonth]);

        ScheduleCandidate unlocated = Assert.Single(result.UnlocatedCandidates);
        Assert.Contains(unlocated.Issues, static issue => issue.Code == ScheduleIssueCode.OutOfTargetMonth);
    }

    [Fact]
    public void Revalidate_BuildResult_RetainsUnlocatedAndAllDuplicateCandidates()
    {
        ScheduleCandidate first = CreateCandidate(3, "first.png");
        ScheduleCandidate duplicate = CreateCandidate(3, "second.png");
        ScheduleCandidate september = CreateCandidate(1, "september.png");
        september.RawDateText = "9/1";
        ScheduleService service = new();
        ScheduleBuildResult initial = service.Build(
            TargetYear,
            TargetMonth,
            [first, duplicate, september]);

        ScheduleBuildResult revalidated = service.Revalidate(TargetYear, TargetMonth, initial);

        Assert.Equal(2, revalidated.DuplicateCandidates.Count);
        Assert.Single(revalidated.UnlocatedCandidates);
        Assert.Contains(revalidated.Issues, static issue => issue.Code == ScheduleIssueCode.MultipleMonths);
    }

    [Fact]
    public void Extract_UnorderedEightColumnTokens_UsesFirstFourAndLastTwoColumns()
    {
        OcrToken[] tokens = CreateOcrTableTokens(splitRoleOneOnFirstRow: false).ToArray();
        OcrToken[] shuffled = tokens
            .OrderBy(static token => StableShuffleKey($"{token.Text}:{token.Bounds.Left}:{token.Bounds.Top}"))
            .ToArray();
        ScheduleExtractor extractor = new();

        ScheduleExtractionResult ordered = extractor.Extract(tokens);
        ScheduleExtractionResult unordered = extractor.Extract(shuffled);

        AssertCandidateTablesEqual(ordered.Candidates, unordered.Candidates);
        ScheduleCandidate first = Assert.Single(
            unordered.Candidates,
            static candidate => candidate.RawDateText == "8/3");
        Assert.Equal("星期一", first.RawWeekdayText);
        Assert.Equal("队长3", first.Role1);
        Assert.Equal("岗位二3", first.Role2);
        Assert.Equal("右一3", first.Role3);
        Assert.Equal("右二3", first.Role4);
        Assert.DoesNotContain("忽略", first.Role3, StringComparison.Ordinal);
        Assert.DoesNotContain("忽略", first.Role4, StringComparison.Ordinal);
    }

    [Fact]
    public void Extract_MultipleTokensInOneCell_JoinsByHorizontalPosition()
    {
        TableLayoutProfile profile = new() { CellTextSeparator = string.Empty };
        OcrToken[] shuffled = CreateOcrTableTokens(splitRoleOneOnFirstRow: true)
            .OrderByDescending(static token => token.Bounds.Left)
            .ToArray();

        ScheduleExtractionResult result = new ScheduleExtractor().Extract(shuffled, profile);
        ScheduleCandidate first = Assert.Single(
            result.Candidates,
            static candidate => candidate.RawDateText == "8/3");

        Assert.Equal("张三", first.Role1);
    }

    [Fact]
    public void Extract_TargetColumnPopulatedOnOnlyOneRow_DoesNotShiftLaterColumns()
    {
        OcrToken[] sparse = CreateOcrTableTokens(splitRoleOneOnFirstRow: false)
            .Where(static token => !token.Text.StartsWith("右一", StringComparison.Ordinal) || token.Text == "右一3")
            .ToArray();

        ScheduleExtractionResult result = new ScheduleExtractor().Extract(sparse);
        ScheduleCandidate first = Assert.Single(result.Candidates, static candidate => candidate.RawDateText == "8/3");
        ScheduleCandidate second = Assert.Single(result.Candidates, static candidate => candidate.RawDateText == "8/4");

        Assert.Equal("右一3", first.Role3);
        Assert.Equal("右二3", first.Role4);
        Assert.Equal(string.Empty, second.Role3);
        Assert.Equal("右二4", second.Role4);
    }

    [Fact]
    public void Extract_RowWithMissingDateAndOneKnownTargetCell_RemainsEditableCandidate()
    {
        OcrToken[] tokens = CreateOcrTableTokens(splitRoleOneOnFirstRow: false)
            .Where(static token =>
                Math.Abs(token.Bounds.CenterY - 0.30F) > 0.001F ||
                token.Text == "队长4")
            .ToArray();

        ScheduleExtractionResult result = new ScheduleExtractor().Extract(tokens);
        ScheduleCandidate candidate = Assert.Single(
            result.Candidates,
            static value => value.Role1 == "队长4");

        Assert.Equal(string.Empty, candidate.RawDateText);
        Assert.Equal(string.Empty, candidate.Role2);
        Assert.Contains(candidate.Issues, static issue => issue.Code == ScheduleIssueCode.InvalidDate);
    }

    [Fact]
    public void Extract_HandDrawnLikeSingletonNoise_DoesNotBecomeOrShiftTargetColumn()
    {
        List<OcrToken> tokens = CreateOcrTableTokens(splitRoleOneOnFirstRow: false).ToList();
        tokens.Add(Token("table.png", "圈", 0.71F, 0.20F, width: 0.006F));

        ScheduleExtractionResult result = new ScheduleExtractor().Extract(tokens);
        ScheduleCandidate first = Assert.Single(result.Candidates, static candidate => candidate.RawDateText == "8/3");

        Assert.Equal("右一3", first.Role3);
        Assert.Equal("右二3", first.Role4);
        Assert.Contains(result.UnassignedTokens, static token => token.Text == "圈");
    }

    [Fact]
    public void Extract_CellSplitBeyondClusteringTolerance_RejoinsWithoutShiftingColumns()
    {
        List<OcrToken> tokens = CreateOcrTableTokens(splitRoleOneOnFirstRow: false)
            .Where(static token => token.Text != "队长3")
            .ToList();
        tokens.Add(Token("table.png", "张", 0.255F, 0.20F, width: 0.006F));
        tokens.Add(Token("table.png", "三", 0.305F, 0.20F, width: 0.006F));

        ScheduleExtractionResult result = new ScheduleExtractor().Extract(
            tokens,
            new TableLayoutProfile { CellTextSeparator = string.Empty });
        ScheduleCandidate first = Assert.Single(result.Candidates, static candidate => candidate.RawDateText == "8/3");

        Assert.Equal("张三", first.Role1);
        Assert.Equal("岗位二3", first.Role2);
        Assert.Equal("右一3", first.Role3);
        Assert.Equal("右二3", first.Role4);
    }

    [Fact]
    public void Extract_DateSplitIntoThreeTokens_StillAnchorsAndParsesRow()
    {
        List<OcrToken> tokens = CreateOcrTableTokens(splitRoleOneOnFirstRow: false)
            .Where(static token => token.Text != "8/3")
            .ToList();
        tokens.Add(Token("table.png", "8", 0.052F, 0.20F, width: 0.005F));
        tokens.Add(Token("table.png", "/", 0.060F, 0.20F, width: 0.003F));
        tokens.Add(Token("table.png", "3", 0.068F, 0.20F, width: 0.005F));

        ScheduleExtractionResult result = new ScheduleExtractor().Extract(tokens);
        ScheduleCandidate first = Assert.Single(result.Candidates, static candidate => candidate.RawDateText == "8/3");

        Assert.Equal("队长3", first.Role1);
        Assert.DoesNotContain(first.Issues, static issue => issue.Code == ScheduleIssueCode.InvalidDate);
    }

    [Fact]
    public void Extract_ExcelColumnLabelsAndRowNumbers_AreIgnoredWithoutShiftingDataColumns()
    {
        List<OcrToken> tokens = CreateOcrTableTokens(splitRoleOneOnFirstRow: false).ToList();
        float[] centers = [0.06F, 0.17F, 0.28F, 0.39F, 0.52F, 0.64F, 0.78F, 0.91F];
        string[] labels = ["A", "B", "C", "D", "E", "F", "G", "H"];
        for (int index = 0; index < labels.Length; index++)
        {
            tokens.Add(Token("table.png", labels[index], centers[index], 0.05F));
        }

        for (int row = 0; row < 3; row++)
        {
            tokens.Add(Token("table.png", (row + 1).ToString(), 0.01F, 0.20F + row * 0.10F));
        }

        ScheduleExtractionResult result = new ScheduleExtractor().Extract(
            tokens.OrderBy(static token => StableShuffleKey($"{token.Text}:{token.Bounds.Left}:{token.Bounds.Top}")));

        Assert.Equal(3, result.Candidates.Count);
        ScheduleCandidate first = Assert.Single(
            result.Candidates,
            static candidate => candidate.RawDateText == "8/3");
        Assert.Equal("星期一", first.RawWeekdayText);
        Assert.Equal("队长3", first.Role1);
        Assert.Equal("岗位二3", first.Role2);
        Assert.Equal("右一3", first.Role3);
        Assert.Equal("右二3", first.Role4);
        Assert.DoesNotContain(result.Candidates, static candidate => candidate.RawDateText == "1");
        Assert.DoesNotContain(result.Candidates, static candidate => candidate.RawWeekdayText == "A");
    }

    private static IEnumerable<ScheduleCandidate> CreateFourDaySeries()
    {
        for (int seriesOffset = 0; seriesOffset < 4; seriesOffset++)
        {
            for (int day = seriesOffset + 1; day <= 31; day += 4)
            {
                yield return CreateCandidate(day, $"source-{seriesOffset + 1}.png");
            }
        }
    }

    private static ScheduleCandidate CreateCandidate(
        int day,
        string sourceImageId = "source.png",
        string? role1 = null)
    {
        DateOnly date = new(TargetYear, TargetMonth, day);
        return new ScheduleCandidate
        {
            RawDateText = $"{TargetMonth}/{day}",
            RawWeekdayText = WeekdayTextParser.ToChinese(date.DayOfWeek),
            Role1 = role1 ?? $"队长{day}",
            Role2 = $"岗位二{day}",
            Role3 = $"岗位三{day}",
            Role4 = $"岗位四{day}",
            SourceImageId = sourceImageId,
            RowCenterY = day / 100F,
        };
    }

    private static IEnumerable<DateOnly> DatesInTargetMonth()
    {
        int days = DateTime.DaysInMonth(TargetYear, TargetMonth);
        for (int day = 1; day <= days; day++)
        {
            yield return new DateOnly(TargetYear, TargetMonth, day);
        }
    }

    private static IEnumerable<OcrToken> CreateOcrTableTokens(bool splitRoleOneOnFirstRow)
    {
        string source = "table.png";
        float[] centers = [0.06F, 0.17F, 0.28F, 0.39F, 0.52F, 0.64F, 0.78F, 0.91F];
        int[] days = [3, 4, 5];
        for (int row = 0; row < days.Length; row++)
        {
            int day = days[row];
            float y = 0.20F + row * 0.10F;
            yield return Token(source, $"8/{day}", centers[0], y);
            yield return Token(source, WeekdayTextParser.ToChinese(new DateOnly(TargetYear, TargetMonth, day).DayOfWeek), centers[1], y);

            if (row == 0 && splitRoleOneOnFirstRow)
            {
                yield return Token(source, "张", centers[2] - 0.004F, y, width: 0.006F);
                yield return Token(source, "三", centers[2] + 0.004F, y, width: 0.006F);
            }
            else
            {
                yield return Token(source, $"队长{day}", centers[2], y);
            }

            yield return Token(source, $"岗位二{day}", centers[3], y);
            yield return Token(source, $"忽略甲{day}", centers[4], y);
            yield return Token(source, $"忽略乙{day}", centers[5], y);
            yield return Token(source, $"右一{day}", centers[6], y);
            yield return Token(source, $"右二{day}", centers[7], y);
        }
    }

    private static OcrToken Token(
        string source,
        string text,
        float centerX,
        float centerY,
        float width = 0.02F,
        float height = 0.02F) =>
        new(
            source,
            text,
            OcrBounds.FromLTRB(
                centerX - width / 2F,
                centerY - height / 2F,
                centerX + width / 2F,
                centerY + height / 2F),
            0.99F);

    private static int StableShuffleKey(string value)
    {
        unchecked
        {
            int hash = 17;
            foreach (char character in value)
            {
                hash = hash * 31 + character;
            }

            return hash;
        }
    }

    private static void AssertCandidateTablesEqual(
        IEnumerable<ScheduleCandidate> expected,
        IEnumerable<ScheduleCandidate> actual)
    {
        static IEnumerable<(string Date, string Weekday, string Role1, string Role2, string Role3, string Role4)> Project(
            IEnumerable<ScheduleCandidate> candidates) =>
            candidates
                .OrderBy(static candidate => candidate.RawDateText, StringComparer.Ordinal)
                .Select(static candidate =>
                    (candidate.RawDateText,
                     candidate.RawWeekdayText,
                     candidate.Role1,
                     candidate.Role2,
                     candidate.Role3,
                     candidate.Role4));

        Assert.Equal(Project(expected), Project(actual));
    }
}
