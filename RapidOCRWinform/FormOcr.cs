using RapidOCRLib;
using RapidOCRLib.Models;
using RapidOCRSchedule.Core;
using RapidOCRSchedule.Excel;
using System.ComponentModel;
using System.Text;

namespace RapidOCRWinform;

public partial class FormOcr : Form
{
    internal const int OcrPadding = 30;
    internal const int OcrMaximumSideLength = 2400;
    internal const float OcrBoxScoreThreshold = 0.45F;
    internal const float OcrBoxThreshold = 0.30F;
    internal const float OcrUnclipRatio = 1.60F;

    private static readonly string[] ModelFileNames =
    [
        "ch_PP-OCRv5_mobile_det.onnx",
        "ch_ppocr_mobile_v2.0_cls_infer.onnx",
        "ch_PP-OCRv5_rec_mobile_infer.onnx",
        "ppocrv5_dict.txt",
    ];

    private readonly AppSettings _settings;
    private readonly List<ImageWorkItem> _images = [];
    private readonly BindingList<EditableScheduleRow> _editableRows = [];
    private readonly BindingSource _rowBinding = new();
    private readonly ScheduleExtractor _extractor = new();
    private readonly ScheduleService _scheduleService = new();
    private readonly List<ScheduleCandidate> _duplicateCandidatesForReview = [];
    private readonly List<EmployeeNameCorrection> _employeeNameCorrections = [];

    private OcrLite? _ocrEngine;
    private ScheduleExtractionResult? _lastExtraction;
    private ScheduleBuildResult? _lastBuild;
    private string _batchDiagnostics = string.Empty;
    private DateTime _appliedMonth;
    private int _appliedRole3Column = -2;
    private int _appliedRole4Column = -1;
    private bool _ocrReady;
    private bool _busy;
    private bool _loaded;
    private bool _hasManualEdits;
    private bool _suppressSettingEvents;

    public FormOcr()
    {
        _settings = AppSettings.Load();
        InitializeComponent();
        _rowBinding.DataSource = _editableRows;
        scheduleGrid.DataSource = _rowBinding;
    }

    private async void FormOcr_Load(object? sender, EventArgs e)
    {
        monthPicker.MinDate = new DateTime(1900, 1, 1);
        monthPicker.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        role3ColumnNumeric.Value = NormalizeColumnSetting(_settings.Role3Column, -2);
        role4ColumnNumeric.Value = NormalizeColumnSetting(_settings.Role4Column, -1);
        _settings.EmployeeNames = EmployeeNameResolver
            .NormalizeEmployeeNames(_settings.EmployeeNames ?? [])
            .ToList();
        UpdateEmployeeRosterStatus();
        RememberAppliedSettings();
        _loaded = true;
        UpdateActionState();
        await InitializeOcrAsync(showSuccess: false);
    }

    private async void initializeModelButton_Click(object? sender, EventArgs e) =>
        await InitializeOcrAsync(showSuccess: true);

    private async Task InitializeOcrAsync(bool showSuccess)
    {
        if (_busy)
        {
            return;
        }

        try
        {
            SetBusy(true, "正在准备内置 OCR 模型…");
            SetModelState(false, "正在准备内置模型…");
            string modelDirectory = await Task.Run(EmbeddedModelProvider.EnsureExtracted);
            OcrLite engine = new();
            int threadCount = Math.Max(1, (int)Math.Ceiling(Environment.ProcessorCount * 0.7));
            try
            {
                await Task.Run(async () =>
                    await engine.InitModels(
                        Path.Combine(modelDirectory, ModelFileNames[0]),
                        Path.Combine(modelDirectory, ModelFileNames[1]),
                        Path.Combine(modelDirectory, ModelFileNames[2]),
                        Path.Combine(modelDirectory, ModelFileNames[3]),
                        threadCount));
            }
            catch
            {
                engine.Dispose();
                throw;
            }

            OcrLite? previousEngine = _ocrEngine;
            _ocrEngine = engine;
            previousEngine?.Dispose();
            _ocrReady = true;
            SetModelState(true, $"内置模型已就绪 · {threadCount} 线程");
            SetStatus("内置 OCR 模型初始化完成");
            if (showSuccess)
            {
                MessageBox.Show(this, "内置 OCR 模型初始化成功。", "模型就绪", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception exception)
        {
            _ocrEngine?.Dispose();
            _ocrEngine = null;
            _ocrReady = false;
            SetModelState(false, "内置模型初始化失败，可重试");
            MessageBox.Show(
                this,
                $"无法准备程序内置的 OCR 模型：\n{exception.Message}\n\n请检查磁盘空间和用户目录权限后重试。",
                "初始化失败",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void FormOcr_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_busy)
        {
            return;
        }

        e.Cancel = true;
        MessageBox.Show(
            this,
            "OCR 正在处理图片或初始化模型，请等待当前操作完成后再关闭窗口。",
            "正在处理",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void selectImagesButton_Click(object? sender, EventArgs e)
    {
        using OpenFileDialog dialog = new()
        {
            Multiselect = true,
            Title = "请选择正好 4 张排班表图片",
            Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff|所有文件|*.*",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        string[] distinctFiles = dialog.FileNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (distinctFiles.Length != 4)
        {
            MessageBox.Show(
                this,
                $"必须一次选择正好 4 张不同的图片；当前选择了 {distinctFiles.Length} 张。",
                "图片数量不正确",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _images.Clear();
        _images.AddRange(distinctFiles.Select(path => new ImageWorkItem { FilePath = path }));
        ClearResults();
        RefreshImageList();
        if (imageListView.Items.Count > 0)
        {
            imageListView.Items[0].Selected = true;
        }

        SetStatus("已选择 4 张图片，等待识别");
        UpdateActionState();
    }

    private void clearImagesButton_Click(object? sender, EventArgs e)
    {
        _images.Clear();
        imageListView.Items.Clear();
        ReplacePreviewImage(null);
        diagnosticsTextBox.Clear();
        ClearResults();
        SetStatus("已清空图片");
        UpdateActionState();
    }

    private async void processButton_Click(object? sender, EventArgs e)
    {
        if (_images.Count != 4)
        {
            MessageBox.Show(this, "请先选择正好 4 张排班图片。", "无法开始", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!_ocrReady || _ocrEngine is null)
        {
            MessageBox.Show(this, "内置 OCR 模型尚未就绪，请重试初始化。", "无法开始", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            SetBusy(true, "开始识别 4 张图片…");
            ClearResults();
            for (int resetIndex = 0; resetIndex < _images.Count; resetIndex++)
            {
                _images[resetIndex].Tokens.Clear();
                _images[resetIndex].Diagnostics = string.Empty;
                _images[resetIndex].Status = "待识别";
                UpdateImageListStatus(resetIndex);
            }

            progressBar.Maximum = _images.Count;
            progressBar.Value = 0;

            for (int index = 0; index < _images.Count; index++)
            {
                ImageWorkItem image = _images[index];
                image.Tokens.Clear();
                image.Status = "识别中…";
                UpdateImageListStatus(index);
                SetStatus($"正在识别第 {index + 1}/4 张：{image.DisplayName}");

                OcrResult? ocrResult = null;
                try
                {
                    Size imageSize = ReadImageSize(image.FilePath);
                    ocrResult = await _ocrEngine.DetectAsync(
                        image.FilePath,
                        OcrPadding,
                        OcrMaximumSideLength,
                        OcrBoxScoreThreshold,
                        OcrBoxThreshold,
                        OcrUnclipRatio,
                        doAngle: false,
                        mostAngle: false);
                    image.Tokens.AddRange(ConvertTokens(image.FilePath, imageSize, ocrResult));
                    image.Diagnostics = FormatOcrDiagnostics(image, ocrResult);
                    image.Status = $"完成 · {image.Tokens.Count} 项";
                }
                catch (Exception exception)
                {
                    image.Status = "失败";
                    image.Diagnostics = $"识别失败：{exception}";
                    throw new InvalidOperationException($"{image.DisplayName} 识别失败：{exception.Message}", exception);
                }
                finally
                {
                    ocrResult?.BoxImg?.Dispose();
                    progressBar.Value = index + 1;
                    UpdateImageListStatus(index);
                }
            }

            ExtractAndBind();
            outputTabs.SelectedTab = resultTabPage;
            SetStatus($"整理完成：{_lastBuild?.Rows.Count ?? 0} 个日期，{_editableRows.Count(static row => row.IssueText.Length > 0)} 行待检查");
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"识别或整理失败：\n{exception.Message}",
                "处理失败",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            SetStatus("处理失败，请检查原图和 OCR 状态");
        }
        finally
        {
            SetBusy(false);
            UpdateDiagnosticsForSelection();
        }
    }

    private void ExtractAndBind()
    {
        TableLayoutProfile profile = CreateLayoutProfile();
        _lastExtraction = _extractor.Extract(_images.SelectMany(static image => image.Tokens), profile);
        ApplyEmployeeNameCorrections(_lastExtraction.Candidates);
        _lastBuild = _scheduleService.Build(
            monthPicker.Value.Year,
            monthPicker.Value.Month,
            _lastExtraction.Candidates,
            alignOcrRowsToCalendar: true);
        CaptureDuplicateCandidates(_lastBuild, replace: true);
        BindBuildResult(_lastBuild);
        RememberAppliedSettings();
        _batchDiagnostics = FormatBatchDiagnostics(_lastExtraction, _lastBuild);
        UpdateDiagnosticsForSelection();
    }

    private void employeeRosterButton_Click(object? sender, EventArgs e)
    {
        using EmployeeRosterDialog dialog = new(_settings.EmployeeNames ?? []);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        List<string> previousNames = _settings.EmployeeNames ?? [];
        List<string> updatedNames = dialog.EmployeeNames.ToList();
        _settings.EmployeeNames = updatedNames;
        if (!_settings.TrySave(out string? errorMessage))
        {
            _settings.EmployeeNames = previousNames;
            MessageBox.Show(
                this,
                $"员工名单无法保存，原配置未更改：\n{errorMessage}",
                "保存失败",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        UpdateEmployeeRosterStatus();
        if (_lastExtraction is not null && !_hasManualEdits)
        {
            try
            {
                ExtractAndBind();
                SetStatus($"员工名单已保存（{updatedNames.Count} 人），当前结果已重新匹配");
            }
            catch (Exception exception)
            {
                SetStatus($"员工名单已保存（{updatedNames.Count} 人），下次识别时生效");
                MessageBox.Show(
                    this,
                    $"名单已保存，但无法重新整理当前结果：\n{exception.Message}",
                    "重新匹配失败",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        else
        {
            string suffix = _hasManualEdits ? "；为保留手工修改，将在下次识别时生效" : string.Empty;
            SetStatus($"员工名单已保存（{updatedNames.Count} 人）{suffix}");
        }
    }

    private void ApplyEmployeeNameCorrections(IEnumerable<ScheduleCandidate> candidates)
    {
        EmployeeNameResolver resolver = new(_settings.EmployeeNames ?? []);
        _employeeNameCorrections.Clear();
        foreach (ScheduleCandidate candidate in candidates)
        {
            candidate.Role1 = ResolveEmployeeName(candidate, nameof(candidate.Role1), candidate.Role1, resolver);
            candidate.Role2 = ResolveEmployeeName(candidate, nameof(candidate.Role2), candidate.Role2, resolver);
            candidate.Role3 = ResolveEmployeeName(candidate, nameof(candidate.Role3), candidate.Role3, resolver);
            candidate.Role4 = ResolveEmployeeName(candidate, nameof(candidate.Role4), candidate.Role4, resolver);
        }
    }

    private string ResolveEmployeeName(
        ScheduleCandidate candidate,
        string role,
        string recognizedName,
        EmployeeNameResolver resolver)
    {
        string correctedName = resolver.Resolve(recognizedName);
        if (!string.Equals(recognizedName.Trim(), correctedName, StringComparison.Ordinal))
        {
            _employeeNameCorrections.Add(new EmployeeNameCorrection(
                candidate.SourceImageId,
                candidate.RawDateText,
                role,
                recognizedName.Trim(),
                correctedName));
        }

        return correctedName;
    }

    private void UpdateEmployeeRosterStatus()
    {
        int count = _settings.EmployeeNames?.Count ?? 0;
        employeeRosterStatusLabel.Text = count > 0 ? $"员工名单：已配置 {count} 人" : "员工名单：尚未配置";
        employeeRosterStatusLabel.ForeColor = count > 0 ? Color.DarkGreen : Color.DimGray;
    }

    private TableLayoutProfile CreateLayoutProfile() => new()
    {
        Role3Column = Decimal.ToInt32(role3ColumnNumeric.Value),
        Role4Column = Decimal.ToInt32(role4ColumnNumeric.Value),
    };

    internal static IEnumerable<OcrToken> ConvertTokens(string sourcePath, Size imageSize, OcrResult result)
    {
        foreach (TextBlock block in result.TextBlocks)
        {
            if (string.IsNullOrWhiteSpace(block.Text) || block.BoxPoints.Count == 0)
            {
                continue;
            }

            float left = Math.Clamp(block.BoxPoints.Min(static point => point.X) - OcrPadding, 0, imageSize.Width);
            float top = Math.Clamp(block.BoxPoints.Min(static point => point.Y) - OcrPadding, 0, imageSize.Height);
            float right = Math.Clamp(block.BoxPoints.Max(static point => point.X) - OcrPadding, 0, imageSize.Width);
            float bottom = Math.Clamp(block.BoxPoints.Max(static point => point.Y) - OcrPadding, 0, imageSize.Height);
            if (right <= left || bottom <= top)
            {
                continue;
            }

            OcrBounds bounds = OcrBounds.FromLTRB(
                left / imageSize.Width,
                top / imageSize.Height,
                right / imageSize.Width,
                bottom / imageSize.Height);
            float characterConfidence = block.CharScores.Count > 0
                ? block.CharScores.Average()
                : block.BoxScore;
            float confidence = Math.Min(block.BoxScore, characterConfidence);
            yield return new OcrToken(sourcePath, block.Text, bounds, confidence);
        }
    }

    private void BindBuildResult(ScheduleBuildResult build)
    {
        _editableRows.RaiseListChangedEvents = false;
        _editableRows.Clear();
        foreach (ScheduleRow source in build.Rows)
        {
            _editableRows.Add(CreateEditableRow(source));
        }

        foreach (ScheduleCandidate candidate in build.UnlocatedCandidates)
        {
            _editableRows.Add(CreateEditableUnlocatedRow(candidate));
        }

        _editableRows.RaiseListChangedEvents = true;
        _rowBinding.ResetBindings(false);

        int issueRows = _editableRows.Count(static row => row.IssueText.Length > 0);
        int extractionIssues = _lastExtraction?.Issues.Count ?? 0;
        int batchIssues = extractionIssues + build.Issues.Count + build.UnlocatedCandidates.Count;
        resultSummaryLabel.Text = batchIssues > 0
            ? $"共 {build.Rows.Count} 个日期，另显示 {build.UnlocatedCandidates.Count} 条未定位记录；{issueRows} 行及 {extractionIssues + build.Issues.Count} 个批次异常待检查。"
            : $"共 {build.Rows.Count} 个日期，{issueRows} 行待检查。可直接修改单元格，完成后导出 Excel。";
        revalidateButton.Enabled = _editableRows.Count > 0 && !_busy;
        exportButton.Enabled = _editableRows.Count > 0 && !_busy;
        scheduleGrid.ClearSelection();
        _hasManualEdits = false;
    }

    private static EditableScheduleRow CreateEditableRow(ScheduleRow source)
    {
        EditableScheduleRow row = new()
        {
            DateText = source.RawDateText,
            Weekday = source.Weekday,
            Role1 = source.Role1,
            Role2 = source.Role2,
            Role3 = source.Role3,
            Role4 = source.Role4,
            SourceText = source.SourceImageId,
            IsMissingPlaceholder = source.IsMissingPlaceholder,
            IssueText = string.Join("；", source.Issues.Select(static issue => issue.Message).Distinct()),
        };
        PopulateIssueMetadata(row, source.Issues);
        if (source.Issues.Any(static issue => issue.Code == ScheduleIssueCode.MissingWorkday))
        {
            row.InvalidProperties.UnionWith(
                [nameof(EditableScheduleRow.DateText), nameof(EditableScheduleRow.Role1), nameof(EditableScheduleRow.Role2), nameof(EditableScheduleRow.Role3), nameof(EditableScheduleRow.Role4)]);
        }

        return row;
    }

    private static EditableScheduleRow CreateEditableUnlocatedRow(ScheduleCandidate source)
    {
        EditableScheduleRow row = new()
        {
            DateText = source.RawDateText,
            Weekday = source.RawWeekdayText,
            Role1 = source.Role1,
            Role2 = source.Role2,
            Role3 = source.Role3,
            Role4 = source.Role4,
            SourceText = source.SourceImageId,
            IsUnlocatedCandidate = true,
            IssueText = $"未定位记录：{string.Join("；", source.Issues.Select(static issue => issue.Message).Distinct())}",
        };
        PopulateIssueMetadata(row, source.Issues);
        row.InvalidProperties.Add(nameof(EditableScheduleRow.DateText));
        return row;
    }

    private static void PopulateIssueMetadata(EditableScheduleRow row, IEnumerable<ScheduleIssue> issues)
    {
        foreach (ScheduleIssue issue in issues)
        {
            string? property = MapIssueProperty(issue.PropertyName);
            if (property is not null)
            {
                row.InvalidProperties.Add(property);
            }

            if (IsPersistentUiIssue(issue.Code))
            {
                row.PersistentIssues.Add(issue);
            }
        }
    }

    private static bool IsPersistentUiIssue(ScheduleIssueCode code) => code is
        ScheduleIssueCode.DuplicateDate or
        ScheduleIssueCode.AmbiguousRow or
        ScheduleIssueCode.AmbiguousColumn or
        ScheduleIssueCode.LowOcrConfidence or
        ScheduleIssueCode.InsufficientColumns;

    private void revalidateButton_Click(object? sender, EventArgs e)
    {
        RevalidateEditedRows();
        SetStatus("已根据当前表格内容重新排序并校验");
    }

    private void RevalidateEditedRows()
    {
        scheduleGrid.EndEdit();
        _rowBinding.EndEdit();
        List<ScheduleRow> edited = [];
        foreach (EditableScheduleRow row in _editableRows)
        {
            ScheduleRow coreRow = new()
            {
                RawDateText = row.DateText,
                Weekday = row.Weekday,
                Role1 = row.Role1,
                Role2 = row.Role2,
                Role3 = row.Role3,
                Role4 = row.Role4,
                SourceImageId = row.SourceText,
                IsMissingPlaceholder = row.IsMissingPlaceholder,
            };
            coreRow.Issues.AddRange(row.PersistentIssues);
            edited.Add(coreRow);
        }
        _lastBuild = _scheduleService.Revalidate(monthPicker.Value.Year, monthPicker.Value.Month, edited);
        CaptureDuplicateCandidates(_lastBuild, replace: false);
        BindBuildResult(_lastBuild);
        if (_lastExtraction is not null)
        {
            _batchDiagnostics = FormatBatchDiagnostics(_lastExtraction, _lastBuild);
            UpdateDiagnosticsForSelection();
        }
    }

    private void exportButton_Click(object? sender, EventArgs e)
    {
        try
        {
            RevalidateEditedRows();
            int issueRows = _editableRows.Count(static row => row.IssueText.Length > 0);
            int batchIssues = (_lastExtraction?.Issues.Count ?? 0) + (_lastBuild?.Issues.Count ?? 0);
            if (issueRows > 0 || batchIssues > 0)
            {
                DialogResult decision = MessageBox.Show(
                    this,
                    $"仍有 {issueRows} 行和 {batchIssues} 个批次级异常待检查。\n\n是否仍按当前可编辑结果导出？",
                    "存在待检查内容",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);
                if (decision != DialogResult.Yes)
                {
                    return;
                }
            }

            List<ExportRow> rows = [];
            foreach (EditableScheduleRow row in _editableRows.Where(static row => !row.IsUnlocatedCandidate))
            {
                if (!DateTextParser.TryCreateDate(monthPicker.Value.Year, row.DateText, out DateOnly date) ||
                    date.Month != monthPicker.Value.Month)
                {
                    throw new InvalidOperationException($"日期“{row.DateText}”无效或不属于目标月份，请先修正。 ");
                }

                rows.Add(new ExportRow(date, row.Role1, row.Role2, row.Role3, row.Role4));
            }

            using SaveFileDialog dialog = new()
            {
                Filter = "Excel 工作簿 (*.xlsx)|*.xlsx",
                AddExtension = true,
                DefaultExt = "xlsx",
                FileName = $"排班表_{monthPicker.Value:yyyy_MM}.xlsx",
                OverwritePrompt = true,
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            ScheduleExcelExporter.Export(dialog.FileName, rows);
            SetStatus($"已导出：{dialog.FileName}");
            MessageBox.Show(
                this,
                $"Excel 已导出：\n{dialog.FileName}",
                "导出完成",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"导出失败：\n{exception.Message}",
                "无法导出",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void scheduleGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _editableRows.Count)
        {
            return;
        }

        EditableScheduleRow row = _editableRows[e.RowIndex];
        if (!row.IssueText.Contains("内容已修改", StringComparison.Ordinal))
        {
            row.IssueText = string.IsNullOrWhiteSpace(row.IssueText)
                ? "内容已修改，请重新校验"
                : $"{row.IssueText}；内容已修改，请重新校验";
        }

        _hasManualEdits = true;
        scheduleGrid.InvalidateRow(e.RowIndex);
    }

    private void scheduleGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _editableRows.Count || e.ColumnIndex < 0)
        {
            return;
        }

        EditableScheduleRow row = _editableRows[e.RowIndex];
        string propertyName = scheduleGrid.Columns[e.ColumnIndex].DataPropertyName;
        if (row.InvalidProperties.Contains(propertyName))
        {
            e.CellStyle.BackColor = Color.MistyRose;
            e.CellStyle.ForeColor = Color.DarkRed;
        }
        else if (propertyName == nameof(EditableScheduleRow.IssueText) && row.IssueText.Length > 0)
        {
            e.CellStyle.BackColor = Color.LemonChiffon;
            e.CellStyle.ForeColor = Color.FromArgb(128, 74, 0);
        }
        else if (row.IsMissingPlaceholder)
        {
            e.CellStyle.BackColor = Color.FromArgb(255, 250, 225);
        }
        else if (row.IsUnlocatedCandidate)
        {
            e.CellStyle.BackColor = Color.FromArgb(255, 235, 235);
        }
    }

    private void scheduleGrid_DataError(object? sender, DataGridViewDataErrorEventArgs e)
    {
        e.ThrowException = false;
        SetStatus($"单元格内容无效：{e.Exception?.Message}");
    }

    private void imageListView_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (imageListView.SelectedItems.Count != 1 || imageListView.SelectedItems[0].Tag is not ImageWorkItem image)
        {
            return;
        }

        try
        {
            ReplacePreviewImage(LoadBitmapWithoutLock(image.FilePath));
        }
        catch (Exception exception)
        {
            ReplacePreviewImage(null);
            SetStatus($"原图预览失败：{exception.Message}");
        }

        UpdateDiagnosticsForSelection();
    }

    private void monthPicker_ValueChanged(object? sender, EventArgs e)
    {
        if (_suppressSettingEvents || !_loaded)
        {
            return;
        }

        if (_lastExtraction is null)
        {
            _appliedMonth = FirstDayOfMonth(monthPicker.Value);
            return;
        }

        if (_busy || !ConfirmDiscardManualEdits("更改目标月份"))
        {
            RevertMonthSetting();
            return;
        }

        try
        {
            _lastBuild = _scheduleService.Build(
                monthPicker.Value.Year,
                monthPicker.Value.Month,
                _lastExtraction.Candidates,
                alignOcrRowsToCalendar: true);
            CaptureDuplicateCandidates(_lastBuild, replace: true);
            BindBuildResult(_lastBuild);
            _appliedMonth = FirstDayOfMonth(monthPicker.Value);
            _batchDiagnostics = FormatBatchDiagnostics(_lastExtraction, _lastBuild);
            UpdateDiagnosticsForSelection();
            SetStatus($"已按 {monthPicker.Value:yyyy 年 M 月} 重新整理");
        }
        catch (Exception exception)
        {
            RevertMonthSetting();
            MessageBox.Show(this, $"无法切换目标月份：\n{exception.Message}", "月份无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void layoutColumnNumeric_ValueChanged(object? sender, EventArgs e)
    {
        if (_suppressSettingEvents)
        {
            return;
        }

        if (sender is NumericUpDown numeric && numeric.Value == 0)
        {
            _suppressSettingEvents = true;
            numeric.Value = ReferenceEquals(numeric, role3ColumnNumeric) ? -2 : -1;
            _suppressSettingEvents = false;
            return;
        }

        if (!_loaded)
        {
            return;
        }

        try
        {
            CreateLayoutProfile().Validate();
        }
        catch (Exception exception)
        {
            RevertColumnSettings();
            MessageBox.Show(this, $"列配置无效：\n{exception.Message}", "无法应用列配置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!ConfirmDiscardManualEdits("更改目标列"))
        {
            RevertColumnSettings();
            return;
        }

        if (!_busy && _images.Any(static image => image.Tokens.Count > 0))
        {
            try
            {
                ExtractAndBind();
                SetStatus("已按新的右侧列序号重新整理，无需重复 OCR");
            }
            catch (Exception exception)
            {
                RevertColumnSettings();
                ClearResults();
                MessageBox.Show(this, $"无法应用列映射：\n{exception.Message}", "列映射失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }
        else
        {
            _appliedRole3Column = Decimal.ToInt32(role3ColumnNumeric.Value);
            _appliedRole4Column = Decimal.ToInt32(role4ColumnNumeric.Value);
        }

        _settings.Role3Column = Decimal.ToInt32(role3ColumnNumeric.Value);
        _settings.Role4Column = Decimal.ToInt32(role4ColumnNumeric.Value);
        _settings.Save();
    }

    private void RefreshImageList()
    {
        imageListView.BeginUpdate();
        imageListView.Items.Clear();
        for (int index = 0; index < _images.Count; index++)
        {
            ImageWorkItem image = _images[index];
            ListViewItem item = new((index + 1).ToString()) { Tag = image, ToolTipText = image.FilePath };
            item.SubItems.Add(image.DisplayName);
            item.SubItems.Add(image.Status);
            imageListView.Items.Add(item);
        }

        imageListView.EndUpdate();
    }

    private void UpdateImageListStatus(int index)
    {
        if (index >= 0 && index < imageListView.Items.Count)
        {
            imageListView.Items[index].SubItems[2].Text = _images[index].Status;
        }
    }

    private void UpdateDiagnosticsForSelection()
    {
        ImageWorkItem? image = imageListView.SelectedItems.Count == 1
            ? imageListView.SelectedItems[0].Tag as ImageWorkItem
            : null;
        StringBuilder builder = new();
        if (image is not null)
        {
            builder.AppendLine(image.FilePath);
            builder.AppendLine(new string('=', 72));
            builder.AppendLine(string.IsNullOrWhiteSpace(image.Diagnostics) ? "尚未识别。" : image.Diagnostics);
        }

        if (!string.IsNullOrWhiteSpace(_batchDiagnostics))
        {
            builder.AppendLine();
            builder.AppendLine("批次整理诊断");
            builder.AppendLine(new string('=', 72));
            builder.Append(_batchDiagnostics);
        }

        diagnosticsTextBox.Text = builder.ToString();
    }

    private static string FormatOcrDiagnostics(ImageWorkItem image, OcrResult result)
    {
        StringBuilder builder = new();
        builder.AppendLine($"OCR 用时：{result.DetectTime:N0} ms；有效文本框：{image.Tokens.Count}");
        foreach (OcrToken token in image.Tokens.OrderBy(static token => token.Bounds.CenterY).ThenBy(static token => token.Bounds.CenterX))
        {
            builder.AppendLine(
                $"[{token.Confidence,5:P0}] x={token.Bounds.CenterX:F3}, y={token.Bounds.CenterY:F3}  {token.Text}");
        }

        return builder.ToString();
    }

    private string FormatBatchDiagnostics(ScheduleExtractionResult extraction, ScheduleBuildResult build)
    {
        StringBuilder builder = new();
        builder.AppendLine($"候选记录：{extraction.Candidates.Count}");
        builder.AppendLine($"未分配 OCR 项：{extraction.UnassignedTokens.Count}");
        builder.AppendLine($"目标日期：{build.Rows.Count}");
        builder.AppendLine($"未定位/跨月记录：{build.UnlocatedCandidates.Count}");
        builder.AppendLine($"同日重复候选：{_duplicateCandidatesForReview.Count}");
        builder.AppendLine($"姓名自动规范：{_employeeNameCorrections.Count}");

        foreach (EmployeeNameCorrection correction in _employeeNameCorrections)
        {
            builder.AppendLine(
                $"[姓名规范] {Path.GetFileName(correction.SourceImageId)} {correction.RawDateText} {correction.Role}: “{correction.OriginalName}” → “{correction.CorrectedName}”");
        }

        IEnumerable<ScheduleIssue> batchIssues = extraction.Issues.Concat(build.Issues);
        foreach (ScheduleIssue issue in batchIssues)
        {
            builder.AppendLine($"[批次异常/{issue.Code}] {issue.Message}");
        }

        foreach (ScheduleCandidate candidate in build.UnlocatedCandidates)
        {
            string issues = string.Join("；", candidate.Issues.Select(static issue => issue.Message).Distinct());
            builder.AppendLine(
                $"[未定位] {Path.GetFileName(candidate.SourceImageId)} y={candidate.RowCenterY:F3} 日期“{candidate.RawDateText}” {issues}");
        }

        foreach (ScheduleCandidate candidate in _duplicateCandidatesForReview)
        {
            builder.AppendLine(
                $"[重复候选 {candidate.RawDateText}] {Path.GetFileName(candidate.SourceImageId)} | {candidate.RawWeekdayText} | {candidate.Role1} | {candidate.Role2} | {candidate.Role3} | {candidate.Role4}");
        }

        foreach (ScheduleRow row in build.Rows.Where(static row => row.Issues.Count > 0))
        {
            builder.AppendLine(
                $"[{row.RawDateText}] {string.Join("；", row.Issues.Select(static issue => issue.Message).Distinct())}");
        }

        return builder.ToString();
    }

    private void CaptureDuplicateCandidates(ScheduleBuildResult build, bool replace)
    {
        if (replace)
        {
            _duplicateCandidatesForReview.Clear();
        }

        foreach (ScheduleCandidate candidate in build.DuplicateCandidates)
        {
            bool alreadyPresent = _duplicateCandidatesForReview.Any(existing =>
                string.Equals(existing.SourceImageId, candidate.SourceImageId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(existing.RawDateText, candidate.RawDateText, StringComparison.Ordinal) &&
                string.Equals(existing.Role1, candidate.Role1, StringComparison.Ordinal) &&
                string.Equals(existing.Role2, candidate.Role2, StringComparison.Ordinal) &&
                string.Equals(existing.Role3, candidate.Role3, StringComparison.Ordinal) &&
                string.Equals(existing.Role4, candidate.Role4, StringComparison.Ordinal));
            if (!alreadyPresent)
            {
                _duplicateCandidatesForReview.Add(candidate);
            }
        }
    }

    private void ClearResults()
    {
        _lastExtraction = null;
        _lastBuild = null;
        _batchDiagnostics = string.Empty;
        _duplicateCandidatesForReview.Clear();
        _employeeNameCorrections.Clear();
        _editableRows.Clear();
        _rowBinding.ResetBindings(false);
        resultSummaryLabel.Text = "尚未整理。异常单元格将用浅红色标记，可直接双击修改。";
        revalidateButton.Enabled = false;
        exportButton.Enabled = false;
        _hasManualEdits = false;
    }

    private bool ConfirmDiscardManualEdits(string action)
    {
        if (!_hasManualEdits)
        {
            return true;
        }

        return MessageBox.Show(
                   this,
                   $"当前表格包含尚未导出的手工修改。{action}会按 OCR 原始结果重新整理并覆盖这些修改。\n\n是否继续？",
                   "确认覆盖手工修改",
                   MessageBoxButtons.YesNo,
                   MessageBoxIcon.Warning,
                   MessageBoxDefaultButton.Button2) == DialogResult.Yes;
    }

    private void RememberAppliedSettings()
    {
        _appliedMonth = FirstDayOfMonth(monthPicker.Value);
        _appliedRole3Column = Decimal.ToInt32(role3ColumnNumeric.Value);
        _appliedRole4Column = Decimal.ToInt32(role4ColumnNumeric.Value);
        _hasManualEdits = false;
    }

    private void RevertMonthSetting()
    {
        _suppressSettingEvents = true;
        monthPicker.Value = _appliedMonth < monthPicker.MinDate ? monthPicker.MinDate : _appliedMonth;
        _suppressSettingEvents = false;
    }

    private void RevertColumnSettings()
    {
        _suppressSettingEvents = true;
        role3ColumnNumeric.Value = _appliedRole3Column;
        role4ColumnNumeric.Value = _appliedRole4Column;
        _suppressSettingEvents = false;
    }

    private static DateTime FirstDayOfMonth(DateTime value) => new(value.Year, value.Month, 1);

    private void SetBusy(bool busy, string? message = null)
    {
        _busy = busy;
        UseWaitCursor = busy;
        progressBar.Visible = busy;
        selectImagesButton.Enabled = !busy;
        clearImagesButton.Enabled = !busy && _images.Count > 0;
        initializeModelButton.Enabled = !busy;
        monthPicker.Enabled = !busy;
        role3ColumnNumeric.Enabled = !busy;
        role4ColumnNumeric.Enabled = !busy;
        employeeRosterButton.Enabled = !busy;
        scheduleGrid.ReadOnly = busy;
        if (message is not null)
        {
            SetStatus(message);
        }

        UpdateActionState();
    }

    private void UpdateActionState()
    {
        processButton.Enabled = !_busy && _ocrReady && _images.Count == 4;
        clearImagesButton.Enabled = !_busy && _images.Count > 0;
        revalidateButton.Enabled = !_busy && _editableRows.Count > 0;
        exportButton.Enabled = !_busy && _editableRows.Count > 0;
    }

    private void SetModelState(bool ready, string text)
    {
        _ocrReady = ready;
        modelStatusLabel.Text = text;
        modelStatusLabel.ForeColor = ready ? Color.DarkGreen : Color.DarkRed;
        modelStateLabel.Text = ready ? "● OCR 模型已就绪" : "● OCR 模型未就绪";
        modelStateLabel.ForeColor = ready ? Color.FromArgb(124, 225, 154) : Color.FromArgb(255, 214, 102);
        UpdateActionState();
    }

    private void SetStatus(string text) => statusLabel.Text = text;

    private static decimal NormalizeColumnSetting(int value, int fallback) =>
        value is >= -50 and <= 50 && value != 0 ? value : fallback;

    private static string? MapIssueProperty(string? propertyName) => propertyName switch
    {
        nameof(ScheduleRow.RawDateText) => nameof(EditableScheduleRow.DateText),
        nameof(ScheduleRow.Weekday) => nameof(EditableScheduleRow.Weekday),
        nameof(ScheduleRow.Role1) => nameof(EditableScheduleRow.Role1),
        nameof(ScheduleRow.Role2) => nameof(EditableScheduleRow.Role2),
        nameof(ScheduleRow.Role3) => nameof(EditableScheduleRow.Role3),
        nameof(ScheduleRow.Role4) => nameof(EditableScheduleRow.Role4),
        _ => null,
    };

    private sealed record EmployeeNameCorrection(
        string SourceImageId,
        string RawDateText,
        string Role,
        string OriginalName,
        string CorrectedName);

    internal static Size ReadImageSize(string path)
    {
        using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using Image image = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: false);
        return image.Size;
    }

    private static Bitmap LoadBitmapWithoutLock(string path)
    {
        using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using Image source = Image.FromStream(stream, useEmbeddedColorManagement: true, validateImageData: false);
        return new Bitmap(source);
    }

    private void ReplacePreviewImage(Image? nextImage)
    {
        Image? previous = previewPictureBox.Image;
        previewPictureBox.Image = nextImage;
        previous?.Dispose();
    }
}
