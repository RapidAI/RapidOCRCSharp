namespace RapidOCRSchedule.Core;

/// <summary>
/// Converts unordered, normalized OCR boxes into schedule-row candidates.
/// It never relies on the OCR engine's return order.
/// </summary>
public sealed class ScheduleExtractor
{
    public ScheduleExtractionResult Extract(
        IEnumerable<OcrToken> tokens,
        TableLayoutProfile? profile = null)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        profile ??= new TableLayoutProfile();
        profile.Validate();

        ScheduleExtractionResult result = new();
        OcrToken[] materialized = tokens
            .Where(static token => !string.IsNullOrWhiteSpace(token.Text))
            .ToArray();

        IGrouping<string, OcrToken>[] sources = materialized
            .GroupBy(static token => token.SourceImageId, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        List<float> sharedColumnCenters = InferSharedColumnCenters(sources, profile, result);

        foreach (IGrouping<string, OcrToken> source in sources)
        {
            ExtractSource(source.Key, source.ToArray(), profile, sharedColumnCenters, result);
        }

        return result;
    }

    private static void ExtractSource(
        string sourceImageId,
        IReadOnlyList<OcrToken> sourceTokens,
        TableLayoutProfile profile,
        IReadOnlyList<float> sharedColumnCenters,
        ScheduleExtractionResult result)
    {
        List<OcrToken> bodyTokens = sourceTokens
            .Where(token => token.Bounds.CenterY >= profile.BodyTop)
            .OrderBy(static token => token.Bounds.CenterY)
            .ThenBy(static token => token.Bounds.CenterX)
            .ToList();

        if (bodyTokens.Count == 0)
        {
            result.Issues.Add(new ScheduleIssue(
                ScheduleIssueCode.UnassignedOcrToken,
                "图片中没有可用于表格解析的 OCR 文本。",
                SourceImageId: sourceImageId));
            return;
        }

        float medianHeight = Median(bodyTokens.Select(static token => token.Bounds.Height).Where(static value => value > 0));
        float rowTolerance = Math.Max(
            profile.Geometry.MinimumRowTolerance,
            medianHeight * profile.Geometry.RowToleranceFactor);
        List<RowCluster> rows = ClusterRows(bodyTokens, rowTolerance);

        float medianWidth = Median(bodyTokens.Select(static token => token.Bounds.Width).Where(static value => value > 0));
        float columnTolerance = Math.Max(
            profile.Geometry.MinimumColumnTolerance,
            medianWidth * profile.Geometry.ColumnToleranceFactor);
        IReadOnlyList<float> columns = sharedColumnCenters;

        int requiredPositiveColumn = profile.TargetColumns.Where(static value => value > 0).DefaultIfEmpty(0).Max();
        int requiredColumnCount = Math.Max(requiredPositiveColumn, profile.TargetColumns.Count);
        if (columns.Count < requiredColumnCount)
        {
            result.Issues.Add(new ScheduleIssue(
                ScheduleIssueCode.InsufficientColumns,
                $"{Path.GetFileName(sourceImageId)} 仅推断出 {columns.Count} 列，无法可靠映射全部 6 个目标字段；请检查图片或调整右侧列序号。",
                SourceImageId: sourceImageId));
        }

        int[] resolvedTargets = profile.TargetColumns
            .Select(column => ResolveColumnIndex(column, columns.Count))
            .ToArray();
        bool targetCollision = resolvedTargets.Where(static index => index >= 0).Distinct().Count() !=
                               resolvedTargets.Count(static index => index >= 0);

        float[] dateRowCenters = rows
            .Where(static row => RowContainsDate(row))
            .Select(static row => row.Center)
            .Order()
            .ToArray();
        float typicalRowGap = Median(
            dateRowCenters.Zip(dateRowCenters.Skip(1), static (top, bottom) => bottom - top)
                .Where(static gap => gap > 0));
        float bodyMinimumY = dateRowCenters.Length > 0
            ? dateRowCenters[0] - Math.Max(typicalRowGap, rowTolerance * 2F)
            : profile.BodyTop;
        float bodyMaximumY = dateRowCenters.Length > 0
            ? dateRowCenters[^1] + Math.Max(typicalRowGap, rowTolerance * 2F)
            : 1F;

        foreach (RowCluster row in rows)
        {
            float assignmentTolerance = GetAssignmentTolerance(columns, columnTolerance);
            Dictionary<int, List<OcrToken>> cells = AssignCells(row, columns, assignmentTolerance, out List<OcrToken> rejectedTokens);
            result.UnassignedTokens.AddRange(rejectedTokens);
            string dateText = JoinCell(cells, resolvedTargets[0], string.Empty);
            string weekdayText = JoinCell(cells, resolvedTargets[1], string.Empty);
            string role1 = JoinCell(cells, resolvedTargets[2], profile.CellTextSeparator);
            string role2 = JoinCell(cells, resolvedTargets[3], profile.CellTextSeparator);
            string role3 = JoinCell(cells, resolvedTargets[4], profile.CellTextSeparator);
            string role4 = JoinCell(cells, resolvedTargets[5], profile.CellTextSeparator);

            if (IsHeaderRow(dateText, weekdayText))
            {
                continue;
            }

            int populatedTargetCells = new[] { dateText, weekdayText, role1, role2, role3, role4 }
                .Count(static value => !string.IsNullOrWhiteSpace(value));
            bool hasDate = DateTextParser.TryParseMonthDay(dateText, out _, out _);
            bool withinDataBody = row.Center >= bodyMinimumY && row.Center <= bodyMaximumY;
            if (!hasDate && (!withinDataBody || populatedTargetCells < profile.Geometry.MinimumPopulatedTargetCells))
            {
                result.UnassignedTokens.AddRange(row.Tokens);
                continue;
            }

            ScheduleCandidate candidate = new()
            {
                SourceImageId = sourceImageId,
                RawDateText = dateText,
                RawWeekdayText = weekdayText,
                Role1 = role1,
                Role2 = role2,
                Role3 = role3,
                Role4 = role4,
                RowCenterY = row.Center,
            };
            candidate.SourceTokens.AddRange(row.Tokens);

            if (!hasDate)
            {
                candidate.Issues.Add(new ScheduleIssue(
                    ScheduleIssueCode.InvalidDate,
                    "日期无法识别，请手动检查。",
                    nameof(ScheduleRow.RawDateText),
                    sourceImageId));
            }

            if (targetCollision || resolvedTargets.Any(static index => index < 0))
            {
                candidate.Issues.Add(new ScheduleIssue(
                    ScheduleIssueCode.AmbiguousColumn,
                    "目标列映射不完整或发生重叠，请调整岗位三、岗位四列序号。",
                    SourceImageId: sourceImageId));
            }

            if (row.CenterSpan > rowTolerance * 0.85F)
            {
                candidate.Issues.Add(new ScheduleIssue(
                    ScheduleIssueCode.AmbiguousRow,
                    "本行文字的纵向位置分散，可能跨越了相邻两行，请对照原图检查。",
                    SourceImageId: sourceImageId));
            }

            if (HasBoundaryAmbiguity(row, columns, resolvedTargets, columnTolerance))
            {
                candidate.Issues.Add(new ScheduleIssue(
                    ScheduleIssueCode.AmbiguousColumn,
                    "本行存在靠近两列边界的文字，列归属需要人工检查。",
                    SourceImageId: sourceImageId));
            }

            if (row.Tokens.Any(token => token.Confidence < profile.Geometry.LowConfidenceThreshold))
            {
                candidate.Issues.Add(new ScheduleIssue(
                    ScheduleIssueCode.LowOcrConfidence,
                    "本行包含低置信度 OCR 文字，请对照原图检查。",
                    SourceImageId: sourceImageId));
            }

            result.Candidates.Add(candidate);
        }

        if (result.UnassignedTokens.Any(token =>
                string.Equals(token.SourceImageId, sourceImageId, StringComparison.OrdinalIgnoreCase)))
        {
            int count = result.UnassignedTokens.Count(token =>
                string.Equals(token.SourceImageId, sourceImageId, StringComparison.OrdinalIgnoreCase));
            result.Issues.Add(new ScheduleIssue(
                ScheduleIssueCode.UnassignedOcrToken,
                $"有 {count} 个 OCR 文本未形成有效排班行，可能是标题、手绘标记或识别噪声。",
                SourceImageId: sourceImageId));
        }
    }

    private static List<RowCluster> ClusterRows(IReadOnlyList<OcrToken> tokens, float tolerance)
    {
        List<RowCluster> rows = [];
        foreach (OcrToken token in tokens)
        {
            RowCluster? best = rows.Count == 0 ? null : rows[^1];
            if (best is null || Math.Abs(token.Bounds.CenterY - best.Center) > tolerance)
            {
                best = new RowCluster();
                rows.Add(best);
            }

            best.Add(token);
        }

        return rows;
    }

    private static List<float> InferSharedColumnCenters(
        IReadOnlyList<IGrouping<string, OcrToken>> sources,
        TableLayoutProfile profile,
        ScheduleExtractionResult result)
    {
        List<PositionedToken> positioned = [];
        List<(RowCluster Row, int GlobalIndex)> fallbackRows = [];
        int globalRowIndex = 0;
        foreach (IGrouping<string, OcrToken> source in sources)
        {
            List<OcrToken> sourceTokens = source
                .Where(token => token.Bounds.CenterY >= profile.BodyTop && !string.IsNullOrWhiteSpace(token.Text))
                .OrderBy(static token => token.Bounds.CenterY)
                .ThenBy(static token => token.Bounds.CenterX)
                .ToList();
            float medianHeight = Median(sourceTokens.Select(static token => token.Bounds.Height).Where(static value => value > 0));
            float rowTolerance = Math.Max(
                profile.Geometry.MinimumRowTolerance,
                medianHeight * profile.Geometry.RowToleranceFactor);
            foreach (RowCluster row in ClusterRows(sourceTokens, rowTolerance))
            {
                fallbackRows.Add((row, globalRowIndex));
                if (IsLikelyTableRow(row))
                {
                    positioned.AddRange(row.Tokens.Select(token => new PositionedToken(token, globalRowIndex)));
                }

                globalRowIndex++;
            }
        }

        if (positioned.Count == 0)
        {
            positioned.AddRange(fallbackRows.SelectMany(
                static entry => entry.Row.Tokens.Select(token => new PositionedToken(token, entry.GlobalIndex))));
        }

        float medianWidth = Median(positioned.Select(static item => item.Token.Bounds.Width).Where(static value => value > 0));
        float tolerance = Math.Max(
            profile.Geometry.MinimumColumnTolerance,
            medianWidth * profile.Geometry.ColumnToleranceFactor);

        List<ColumnCluster> clusters = [];
        foreach (PositionedToken item in positioned.OrderBy(static item => item.Token.Bounds.CenterX))
        {
            ColumnCluster? nearest = clusters
                .Where(cluster => Math.Abs(cluster.Center - item.Token.Bounds.CenterX) <= tolerance)
                .OrderBy(cluster => Math.Abs(cluster.Center - item.Token.Bounds.CenterX))
                .FirstOrDefault();
            if (nearest is null)
            {
                nearest = new ColumnCluster();
                clusters.Add(nearest);
            }

            nearest.Add(item);
        }

        List<ColumnCluster> ordered = clusters.OrderBy(static cluster => cluster.Center).ToList();
        int requiredColumnCount = Math.Max(
            profile.TargetColumns.Where(static value => value > 0).DefaultIfEmpty(0).Max(),
            profile.TargetColumns.Count);
        List<ColumnCluster> recurring = ordered.Where(static cluster => cluster.RowCount >= 2).ToList();
        if (recurring.Count < requiredColumnCount)
        {
            return ordered.Select(static cluster => cluster.Center).ToList();
        }

        float typicalGap = Median(
            recurring.Zip(recurring.Skip(1), static (left, right) => right.Center - left.Center)
                .Where(gap => gap > tolerance * 1.2F));
        List<ColumnCluster> selected = [.. recurring];
        int retainedSparse = 0;
        foreach (ColumnCluster sparse in ordered.Where(static cluster => cluster.RowCount == 1))
        {
            if (FitsMissingGridPosition(sparse.Center, recurring, typicalGap))
            {
                selected.Add(sparse);
                retainedSparse++;
            }
        }

        if (retainedSparse > 0)
        {
            result.Issues.Add(new ScheduleIssue(
                ScheduleIssueCode.AmbiguousColumn,
                $"列模型中有 {retainedSparse} 列只在单行出现，已按表格间距保留，请重点检查这些列。"));
        }

        return selected.OrderBy(static cluster => cluster.Center).Select(static cluster => cluster.Center).ToList();
    }

    private static bool FitsMissingGridPosition(
        float center,
        IReadOnlyList<ColumnCluster> recurring,
        float typicalGap)
    {
        if (typicalGap <= 0 || recurring.Count < 2)
        {
            return false;
        }

        ColumnCluster? left = recurring.LastOrDefault(cluster => cluster.Center < center);
        ColumnCluster? right = recurring.FirstOrDefault(cluster => cluster.Center > center);
        const float minimumFactor = 0.42F;
        const float maximumFactor = 1.65F;

        if (left is not null && right is not null)
        {
            float leftGap = center - left.Center;
            float rightGap = right.Center - center;
            return right.Center - left.Center > typicalGap * 1.55F &&
                   leftGap >= typicalGap * minimumFactor && leftGap <= typicalGap * maximumFactor &&
                   rightGap >= typicalGap * minimumFactor && rightGap <= typicalGap * maximumFactor;
        }

        float edgeGap = left is not null ? center - left.Center : right!.Center - center;
        return edgeGap >= typicalGap * minimumFactor && edgeGap <= typicalGap * maximumFactor;
    }

    private static bool IsLikelyTableRow(RowCluster row)
    {
        if (RowContainsDate(row))
        {
            return true;
        }

        string combined = string.Concat(
            row.Tokens.OrderBy(static token => token.Bounds.CenterX).Select(static token => token.Text));
        return combined.Contains("日期", StringComparison.Ordinal) &&
               (combined.Contains("星期", StringComparison.Ordinal) || combined.Contains("周", StringComparison.Ordinal));
    }

    private static bool RowContainsDate(RowCluster row)
    {
        OcrToken[] ordered = row.Tokens.OrderBy(static token => token.Bounds.CenterX).ToArray();
        for (int count = 1; count <= Math.Min(3, ordered.Length); count++)
        {
            string candidate = string.Concat(ordered.Take(count).Select(static token => token.Text));
            if (DateTextParser.TryParseMonthDay(candidate, out _, out _))
            {
                return true;
            }
        }

        return false;
    }

    private static Dictionary<int, List<OcrToken>> AssignCells(
        RowCluster row,
        IReadOnlyList<float> columns,
        float assignmentTolerance,
        out List<OcrToken> rejectedTokens)
    {
        Dictionary<int, List<OcrToken>> cells = [];
        rejectedTokens = [];
        if (columns.Count == 0)
        {
            rejectedTokens.AddRange(row.Tokens);
            return cells;
        }

        foreach (OcrToken token in row.Tokens)
        {
            int columnIndex = Enumerable.Range(0, columns.Count)
                .OrderBy(index => Math.Abs(columns[index] - token.Bounds.CenterX))
                .First();
            if (Math.Abs(columns[columnIndex] - token.Bounds.CenterX) > assignmentTolerance)
            {
                rejectedTokens.Add(token);
                continue;
            }

            if (!cells.TryGetValue(columnIndex, out List<OcrToken>? cellTokens))
            {
                cellTokens = [];
                cells[columnIndex] = cellTokens;
            }

            cellTokens.Add(token);
        }

        return cells;
    }

    private static float GetAssignmentTolerance(IReadOnlyList<float> columns, float columnTolerance)
    {
        float typicalGap = Median(
            columns.Zip(columns.Skip(1), static (left, right) => right - left)
                .Where(static gap => gap > 0));
        float preferred = Math.Max(0.018F, columnTolerance * 2.2F);
        return typicalGap > 0 ? Math.Min(preferred, typicalGap * 0.42F) : preferred;
    }

    private static string JoinCell(
        IReadOnlyDictionary<int, List<OcrToken>> cells,
        int columnIndex,
        string separator)
    {
        if (columnIndex < 0 || !cells.TryGetValue(columnIndex, out List<OcrToken>? tokens))
        {
            return string.Empty;
        }

        return string.Join(
            separator,
            tokens.OrderBy(static token => token.Bounds.Left)
                .Select(static token => token.Text.Trim())
                .Where(static text => text.Length > 0));
    }

    private static bool HasBoundaryAmbiguity(
        RowCluster row,
        IReadOnlyList<float> columns,
        IReadOnlyList<int> targetColumns,
        float columnTolerance)
    {
        if (columns.Count < 2)
        {
            return true;
        }

        HashSet<int> targetSet = targetColumns.Where(static index => index >= 0).ToHashSet();
        foreach (OcrToken token in row.Tokens)
        {
            int[] nearest = Enumerable.Range(0, columns.Count)
                .OrderBy(index => Math.Abs(columns[index] - token.Bounds.CenterX))
                .Take(2)
                .ToArray();
            if (nearest.Length < 2 || !targetSet.Contains(nearest[0]))
            {
                continue;
            }

            float firstDistance = Math.Abs(columns[nearest[0]] - token.Bounds.CenterX);
            float secondDistance = Math.Abs(columns[nearest[1]] - token.Bounds.CenterX);
            if (secondDistance - firstDistance < columnTolerance * 0.22F)
            {
                return true;
            }
        }

        return false;
    }

    private static int ResolveColumnIndex(int configuredIndex, int columnCount) =>
        configuredIndex > 0 ? configuredIndex - 1 : columnCount + configuredIndex;

    private static bool IsHeaderRow(string dateText, string weekdayText)
    {
        string date = dateText.Replace(" ", string.Empty, StringComparison.Ordinal);
        string weekday = weekdayText.Replace(" ", string.Empty, StringComparison.Ordinal);
        return (date.Contains("日期", StringComparison.Ordinal) || date == "日") &&
               (weekday.Contains("星期", StringComparison.Ordinal) || weekday.Contains("周", StringComparison.Ordinal));
    }

    private static float Median(IEnumerable<float> values)
    {
        float[] sorted = values.Order().ToArray();
        if (sorted.Length == 0)
        {
            return 0F;
        }

        int middle = sorted.Length / 2;
        return sorted.Length % 2 == 0
            ? (sorted[middle - 1] + sorted[middle]) / 2F
            : sorted[middle];
    }

    private sealed class RowCluster
    {
        private float _centerTotal;
        private float _minimumCenter = float.PositiveInfinity;
        private float _maximumCenter = float.NegativeInfinity;

        public List<OcrToken> Tokens { get; } = [];
        public float Center => Tokens.Count == 0 ? 0F : _centerTotal / Tokens.Count;
        public float CenterSpan => Tokens.Count == 0 ? 0F : _maximumCenter - _minimumCenter;

        public void Add(OcrToken token)
        {
            Tokens.Add(token);
            _centerTotal += token.Bounds.CenterY;
            _minimumCenter = Math.Min(_minimumCenter, token.Bounds.CenterY);
            _maximumCenter = Math.Max(_maximumCenter, token.Bounds.CenterY);
        }
    }

    private sealed class ColumnCluster
    {
        private float _centerTotal;

        public List<PositionedToken> Items { get; } = [];
        public float Center => Items.Count == 0 ? 0F : _centerTotal / Items.Count;
        public int RowCount => Items.Select(static item => item.RowIndex).Distinct().Count();

        public void Add(PositionedToken item)
        {
            Items.Add(item);
            _centerTotal += item.Token.Bounds.CenterX;
        }
    }

    private sealed record PositionedToken(OcrToken Token, int RowIndex);
}
