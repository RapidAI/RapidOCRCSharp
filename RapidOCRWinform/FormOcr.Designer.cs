#nullable disable

namespace RapidOCRWinform;

partial class FormOcr
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _ocrEngine?.Dispose();
            previewPictureBox.Image?.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        rootLayout = new TableLayoutPanel();
        headerPanel = new Panel();
        modelStateLabel = new Label();
        subtitleLabel = new Label();
        titleLabel = new Label();
        mainSplit = new SplitContainer();
        leftPanel = new Panel();
        processButton = new Button();
        modelGroupBox = new GroupBox();
        modelStatusLabel = new Label();
        initializeModelButton = new Button();
        browseModelButton = new Button();
        modelPathTextBox = new TextBox();
        modelPathLabel = new Label();
        settingsGroupBox = new GroupBox();
        layoutHintLabel = new Label();
        role4ColumnNumeric = new NumericUpDown();
        role4ColumnLabel = new Label();
        role3ColumnNumeric = new NumericUpDown();
        role3ColumnLabel = new Label();
        monthPicker = new DateTimePicker();
        targetMonthLabel = new Label();
        imagesGroupBox = new GroupBox();
        clearImagesButton = new Button();
        selectImagesButton = new Button();
        imageListView = new ListView();
        imageNumberColumn = new ColumnHeader();
        imageNameColumn = new ColumnHeader();
        imageStatusColumn = new ColumnHeader();
        outputTabs = new TabControl();
        resultTabPage = new TabPage();
        resultLayout = new TableLayoutPanel();
        resultToolbarPanel = new Panel();
        exportButton = new Button();
        revalidateButton = new Button();
        resultSummaryLabel = new Label();
        scheduleGrid = new DataGridView();
        dateColumn = new DataGridViewTextBoxColumn();
        weekdayColumn = new DataGridViewTextBoxColumn();
        role1Column = new DataGridViewTextBoxColumn();
        role2Column = new DataGridViewTextBoxColumn();
        role3Column = new DataGridViewTextBoxColumn();
        role4Column = new DataGridViewTextBoxColumn();
        issueColumn = new DataGridViewTextBoxColumn();
        sourceTabPage = new TabPage();
        sourceSplit = new SplitContainer();
        previewPictureBox = new PictureBox();
        diagnosticsTextBox = new TextBox();
        statusStrip = new StatusStrip();
        progressBar = new ToolStripProgressBar();
        statusLabel = new ToolStripStatusLabel();
        rootLayout.SuspendLayout();
        headerPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)mainSplit).BeginInit();
        mainSplit.Panel1.SuspendLayout();
        mainSplit.Panel2.SuspendLayout();
        mainSplit.SuspendLayout();
        leftPanel.SuspendLayout();
        modelGroupBox.SuspendLayout();
        settingsGroupBox.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)role4ColumnNumeric).BeginInit();
        ((System.ComponentModel.ISupportInitialize)role3ColumnNumeric).BeginInit();
        imagesGroupBox.SuspendLayout();
        outputTabs.SuspendLayout();
        resultTabPage.SuspendLayout();
        resultLayout.SuspendLayout();
        resultToolbarPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)scheduleGrid).BeginInit();
        sourceTabPage.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)sourceSplit).BeginInit();
        sourceSplit.Panel1.SuspendLayout();
        sourceSplit.Panel2.SuspendLayout();
        sourceSplit.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)previewPictureBox).BeginInit();
        statusStrip.SuspendLayout();
        SuspendLayout();
        // 
        // rootLayout
        // 
        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(headerPanel, 0, 0);
        rootLayout.Controls.Add(mainSplit, 0, 1);
        rootLayout.Controls.Add(statusStrip, 0, 2);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Location = new Point(0, 0);
        rootLayout.Margin = new Padding(0);
        rootLayout.Name = "rootLayout";
        rootLayout.RowCount = 3;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        rootLayout.Size = new Size(1380, 860);
        rootLayout.TabIndex = 0;
        // 
        // headerPanel
        // 
        headerPanel.BackColor = Color.FromArgb(35, 61, 92);
        headerPanel.Controls.Add(modelStateLabel);
        headerPanel.Controls.Add(subtitleLabel);
        headerPanel.Controls.Add(titleLabel);
        headerPanel.Dock = DockStyle.Fill;
        headerPanel.Location = new Point(0, 0);
        headerPanel.Margin = new Padding(0);
        headerPanel.Name = "headerPanel";
        headerPanel.Size = new Size(1380, 78);
        headerPanel.TabIndex = 0;
        // 
        // modelStateLabel
        // 
        modelStateLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        modelStateLabel.AutoSize = true;
        modelStateLabel.ForeColor = Color.FromArgb(255, 214, 102);
        modelStateLabel.Location = new Point(1185, 29);
        modelStateLabel.Name = "modelStateLabel";
        modelStateLabel.Size = new Size(147, 20);
        modelStateLabel.TabIndex = 2;
        modelStateLabel.Text = "● OCR 模型未初始化";
        // 
        // subtitleLabel
        // 
        subtitleLabel.AutoSize = true;
        subtitleLabel.ForeColor = Color.FromArgb(205, 220, 235);
        subtitleLabel.Location = new Point(28, 48);
        subtitleLabel.Name = "subtitleLabel";
        subtitleLabel.Size = new Size(417, 20);
        subtitleLabel.TabIndex = 1;
        subtitleLabel.Text = "导入 4 张排班图片，按日期穿插合并、标记异常并导出 Excel";
        // 
        // titleLabel
        // 
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold);
        titleLabel.ForeColor = Color.White;
        titleLabel.Location = new Point(24, 9);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(258, 39);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "本地排班图片整理";
        // 
        // mainSplit
        // 
        mainSplit.Dock = DockStyle.Fill;
        mainSplit.FixedPanel = FixedPanel.Panel1;
        mainSplit.Location = new Point(0, 78);
        mainSplit.Margin = new Padding(0);
        mainSplit.Name = "mainSplit";
        // 
        // mainSplit.Panel1
        // 
        mainSplit.Panel1.Controls.Add(leftPanel);
        mainSplit.Panel1MinSize = 380;
        // 
        // mainSplit.Panel2
        // 
        mainSplit.Panel2.Controls.Add(outputTabs);
        mainSplit.Panel2MinSize = 620;
        mainSplit.Size = new Size(1380, 756);
        mainSplit.SplitterDistance = 380;
        mainSplit.TabIndex = 1;
        // 
        // leftPanel
        // 
        leftPanel.AutoScroll = true;
        leftPanel.BackColor = Color.FromArgb(245, 247, 250);
        leftPanel.Controls.Add(processButton);
        leftPanel.Controls.Add(modelGroupBox);
        leftPanel.Controls.Add(settingsGroupBox);
        leftPanel.Controls.Add(imagesGroupBox);
        leftPanel.Dock = DockStyle.Fill;
        leftPanel.Location = new Point(0, 0);
        leftPanel.Name = "leftPanel";
        leftPanel.Padding = new Padding(12);
        leftPanel.Size = new Size(380, 756);
        leftPanel.TabIndex = 0;
        // 
        // processButton
        // 
        processButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        processButton.BackColor = Color.FromArgb(28, 112, 191);
        processButton.Enabled = false;
        processButton.FlatAppearance.BorderSize = 0;
        processButton.FlatStyle = FlatStyle.Flat;
        processButton.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
        processButton.ForeColor = Color.White;
        processButton.Location = new Point(15, 683);
        processButton.Name = "processButton";
        processButton.Size = new Size(348, 48);
        processButton.TabIndex = 3;
        processButton.Text = "识别并整理";
        processButton.UseVisualStyleBackColor = false;
        processButton.Click += processButton_Click;
        // 
        // modelGroupBox
        // 
        modelGroupBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        modelGroupBox.Controls.Add(modelStatusLabel);
        modelGroupBox.Controls.Add(initializeModelButton);
        modelGroupBox.Controls.Add(browseModelButton);
        modelGroupBox.Controls.Add(modelPathTextBox);
        modelGroupBox.Controls.Add(modelPathLabel);
        modelGroupBox.Location = new Point(15, 512);
        modelGroupBox.Name = "modelGroupBox";
        modelGroupBox.Size = new Size(348, 157);
        modelGroupBox.TabIndex = 2;
        modelGroupBox.TabStop = false;
        modelGroupBox.Text = "OCR 模型";
        // 
        // modelStatusLabel
        // 
        modelStatusLabel.AutoEllipsis = true;
        modelStatusLabel.ForeColor = Color.DimGray;
        modelStatusLabel.Location = new Point(12, 122);
        modelStatusLabel.Name = "modelStatusLabel";
        modelStatusLabel.Size = new Size(324, 25);
        modelStatusLabel.TabIndex = 4;
        modelStatusLabel.Text = "等待初始化";
        // 
        // initializeModelButton
        // 
        initializeModelButton.Location = new Point(228, 81);
        initializeModelButton.Name = "initializeModelButton";
        initializeModelButton.Size = new Size(108, 34);
        initializeModelButton.TabIndex = 3;
        initializeModelButton.Text = "重新初始化";
        initializeModelButton.UseVisualStyleBackColor = true;
        initializeModelButton.Click += initializeModelButton_Click;
        // 
        // browseModelButton
        // 
        browseModelButton.Location = new Point(228, 41);
        browseModelButton.Name = "browseModelButton";
        browseModelButton.Size = new Size(108, 34);
        browseModelButton.TabIndex = 2;
        browseModelButton.Text = "选择目录";
        browseModelButton.UseVisualStyleBackColor = true;
        browseModelButton.Click += browseModelButton_Click;
        // 
        // modelPathTextBox
        // 
        modelPathTextBox.Location = new Point(12, 44);
        modelPathTextBox.Name = "modelPathTextBox";
        modelPathTextBox.Size = new Size(208, 27);
        modelPathTextBox.TabIndex = 1;
        modelPathTextBox.TextChanged += modelPathTextBox_TextChanged;
        // 
        // modelPathLabel
        // 
        modelPathLabel.AutoSize = true;
        modelPathLabel.Location = new Point(12, 23);
        modelPathLabel.Name = "modelPathLabel";
        modelPathLabel.Size = new Size(69, 20);
        modelPathLabel.TabIndex = 0;
        modelPathLabel.Text = "模型目录";
        // 
        // settingsGroupBox
        // 
        settingsGroupBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        settingsGroupBox.Controls.Add(layoutHintLabel);
        settingsGroupBox.Controls.Add(role4ColumnNumeric);
        settingsGroupBox.Controls.Add(role4ColumnLabel);
        settingsGroupBox.Controls.Add(role3ColumnNumeric);
        settingsGroupBox.Controls.Add(role3ColumnLabel);
        settingsGroupBox.Controls.Add(monthPicker);
        settingsGroupBox.Controls.Add(targetMonthLabel);
        settingsGroupBox.Location = new Point(15, 324);
        settingsGroupBox.Name = "settingsGroupBox";
        settingsGroupBox.Size = new Size(348, 174);
        settingsGroupBox.TabIndex = 1;
        settingsGroupBox.TabStop = false;
        settingsGroupBox.Text = "整理设置";
        // 
        // layoutHintLabel
        // 
        layoutHintLabel.ForeColor = Color.DimGray;
        layoutHintLabel.Location = new Point(14, 129);
        layoutHintLabel.Name = "layoutHintLabel";
        layoutHintLabel.Size = new Size(318, 41);
        layoutHintLabel.TabIndex = 6;
        layoutHintLabel.Text = "列序号从 1 开始；-1 表示最后一列，-2 表示倒数第二列。前四列固定为日期、星期、岗位一、岗位二。";
        // 
        // role4ColumnNumeric
        // 
        role4ColumnNumeric.Location = new Point(255, 88);
        role4ColumnNumeric.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
        role4ColumnNumeric.Minimum = new decimal(new int[] { 50, 0, 0, int.MinValue });
        role4ColumnNumeric.Name = "role4ColumnNumeric";
        role4ColumnNumeric.Size = new Size(77, 27);
        role4ColumnNumeric.TabIndex = 5;
        role4ColumnNumeric.Value = new decimal(new int[] { 1, 0, 0, int.MinValue });
        role4ColumnNumeric.ValueChanged += layoutColumnNumeric_ValueChanged;
        // 
        // role4ColumnLabel
        // 
        role4ColumnLabel.AutoSize = true;
        role4ColumnLabel.Location = new Point(181, 91);
        role4ColumnLabel.Name = "role4ColumnLabel";
        role4ColumnLabel.Size = new Size(69, 20);
        role4ColumnLabel.TabIndex = 4;
        role4ColumnLabel.Text = "岗位四列";
        // 
        // role3ColumnNumeric
        // 
        role3ColumnNumeric.Location = new Point(91, 88);
        role3ColumnNumeric.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
        role3ColumnNumeric.Minimum = new decimal(new int[] { 50, 0, 0, int.MinValue });
        role3ColumnNumeric.Name = "role3ColumnNumeric";
        role3ColumnNumeric.Size = new Size(77, 27);
        role3ColumnNumeric.TabIndex = 3;
        role3ColumnNumeric.Value = new decimal(new int[] { 2, 0, 0, int.MinValue });
        role3ColumnNumeric.ValueChanged += layoutColumnNumeric_ValueChanged;
        // 
        // role3ColumnLabel
        // 
        role3ColumnLabel.AutoSize = true;
        role3ColumnLabel.Location = new Point(14, 91);
        role3ColumnLabel.Name = "role3ColumnLabel";
        role3ColumnLabel.Size = new Size(69, 20);
        role3ColumnLabel.TabIndex = 2;
        role3ColumnLabel.Text = "岗位三列";
        // 
        // monthPicker
        // 
        monthPicker.CustomFormat = "yyyy 年 M 月";
        monthPicker.Format = DateTimePickerFormat.Custom;
        monthPicker.Location = new Point(91, 39);
        monthPicker.Name = "monthPicker";
        monthPicker.ShowUpDown = true;
        monthPicker.Size = new Size(241, 27);
        monthPicker.TabIndex = 1;
        monthPicker.ValueChanged += monthPicker_ValueChanged;
        // 
        // targetMonthLabel
        // 
        targetMonthLabel.AutoSize = true;
        targetMonthLabel.Location = new Point(14, 42);
        targetMonthLabel.Name = "targetMonthLabel";
        targetMonthLabel.Size = new Size(69, 20);
        targetMonthLabel.TabIndex = 0;
        targetMonthLabel.Text = "目标月份";
        // 
        // imagesGroupBox
        // 
        imagesGroupBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        imagesGroupBox.Controls.Add(clearImagesButton);
        imagesGroupBox.Controls.Add(selectImagesButton);
        imagesGroupBox.Controls.Add(imageListView);
        imagesGroupBox.Location = new Point(15, 12);
        imagesGroupBox.Name = "imagesGroupBox";
        imagesGroupBox.Size = new Size(348, 298);
        imagesGroupBox.TabIndex = 0;
        imagesGroupBox.TabStop = false;
        imagesGroupBox.Text = "排班图片（必须正好 4 张）";
        // 
        // clearImagesButton
        // 
        clearImagesButton.Location = new Point(228, 250);
        clearImagesButton.Name = "clearImagesButton";
        clearImagesButton.Size = new Size(108, 34);
        clearImagesButton.TabIndex = 2;
        clearImagesButton.Text = "清空";
        clearImagesButton.UseVisualStyleBackColor = true;
        clearImagesButton.Click += clearImagesButton_Click;
        // 
        // selectImagesButton
        // 
        selectImagesButton.Location = new Point(12, 250);
        selectImagesButton.Name = "selectImagesButton";
        selectImagesButton.Size = new Size(208, 34);
        selectImagesButton.TabIndex = 1;
        selectImagesButton.Text = "选择 4 张图片…";
        selectImagesButton.UseVisualStyleBackColor = true;
        selectImagesButton.Click += selectImagesButton_Click;
        // 
        // imageListView
        // 
        imageListView.Columns.AddRange(new ColumnHeader[] { imageNumberColumn, imageNameColumn, imageStatusColumn });
        imageListView.FullRowSelect = true;
        imageListView.GridLines = true;
        imageListView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        imageListView.Location = new Point(12, 29);
        imageListView.MultiSelect = false;
        imageListView.Name = "imageListView";
        imageListView.ShowItemToolTips = true;
        imageListView.Size = new Size(324, 211);
        imageListView.TabIndex = 0;
        imageListView.UseCompatibleStateImageBehavior = false;
        imageListView.View = View.Details;
        imageListView.SelectedIndexChanged += imageListView_SelectedIndexChanged;
        // 
        // imageNumberColumn
        // 
        imageNumberColumn.Text = "#";
        imageNumberColumn.Width = 32;
        // 
        // imageNameColumn
        // 
        imageNameColumn.Text = "文件";
        imageNameColumn.Width = 174;
        // 
        // imageStatusColumn
        // 
        imageStatusColumn.Text = "状态";
        imageStatusColumn.Width = 112;
        // 
        // outputTabs
        // 
        outputTabs.Controls.Add(resultTabPage);
        outputTabs.Controls.Add(sourceTabPage);
        outputTabs.Dock = DockStyle.Fill;
        outputTabs.Location = new Point(0, 0);
        outputTabs.Name = "outputTabs";
        outputTabs.SelectedIndex = 0;
        outputTabs.Size = new Size(996, 756);
        outputTabs.TabIndex = 0;
        // 
        // resultTabPage
        // 
        resultTabPage.Controls.Add(resultLayout);
        resultTabPage.Location = new Point(4, 29);
        resultTabPage.Name = "resultTabPage";
        resultTabPage.Padding = new Padding(8);
        resultTabPage.Size = new Size(988, 723);
        resultTabPage.TabIndex = 0;
        resultTabPage.Text = "整理结果 / 可编辑";
        resultTabPage.UseVisualStyleBackColor = true;
        // 
        // resultLayout
        // 
        resultLayout.ColumnCount = 1;
        resultLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        resultLayout.Controls.Add(resultToolbarPanel, 0, 0);
        resultLayout.Controls.Add(scheduleGrid, 0, 1);
        resultLayout.Dock = DockStyle.Fill;
        resultLayout.Location = new Point(8, 8);
        resultLayout.Name = "resultLayout";
        resultLayout.RowCount = 2;
        resultLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
        resultLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        resultLayout.Size = new Size(972, 707);
        resultLayout.TabIndex = 0;
        // 
        // resultToolbarPanel
        // 
        resultToolbarPanel.Controls.Add(exportButton);
        resultToolbarPanel.Controls.Add(revalidateButton);
        resultToolbarPanel.Controls.Add(resultSummaryLabel);
        resultToolbarPanel.Dock = DockStyle.Fill;
        resultToolbarPanel.Location = new Point(0, 0);
        resultToolbarPanel.Margin = new Padding(0);
        resultToolbarPanel.Name = "resultToolbarPanel";
        resultToolbarPanel.Size = new Size(972, 58);
        resultToolbarPanel.TabIndex = 0;
        // 
        // exportButton
        // 
        exportButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        exportButton.BackColor = Color.FromArgb(42, 139, 87);
        exportButton.Enabled = false;
        exportButton.FlatAppearance.BorderSize = 0;
        exportButton.FlatStyle = FlatStyle.Flat;
        exportButton.ForeColor = Color.White;
        exportButton.Location = new Point(836, 11);
        exportButton.Name = "exportButton";
        exportButton.Size = new Size(126, 36);
        exportButton.TabIndex = 2;
        exportButton.Text = "导出 Excel…";
        exportButton.UseVisualStyleBackColor = false;
        exportButton.Click += exportButton_Click;
        // 
        // revalidateButton
        // 
        revalidateButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        revalidateButton.Enabled = false;
        revalidateButton.Location = new Point(704, 11);
        revalidateButton.Name = "revalidateButton";
        revalidateButton.Size = new Size(126, 36);
        revalidateButton.TabIndex = 1;
        revalidateButton.Text = "重新校验";
        revalidateButton.UseVisualStyleBackColor = true;
        revalidateButton.Click += revalidateButton_Click;
        // 
        // resultSummaryLabel
        // 
        resultSummaryLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        resultSummaryLabel.AutoEllipsis = true;
        resultSummaryLabel.Location = new Point(8, 11);
        resultSummaryLabel.Name = "resultSummaryLabel";
        resultSummaryLabel.Size = new Size(682, 37);
        resultSummaryLabel.TabIndex = 0;
        resultSummaryLabel.Text = "尚未整理。异常单元格将用浅红色标记，可直接双击修改。";
        resultSummaryLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // scheduleGrid
        // 
        scheduleGrid.AllowUserToAddRows = false;
        scheduleGrid.AllowUserToDeleteRows = false;
        scheduleGrid.AutoGenerateColumns = false;
        scheduleGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        scheduleGrid.BackgroundColor = Color.White;
        scheduleGrid.ColumnHeadersHeight = 38;
        scheduleGrid.Columns.AddRange(new DataGridViewColumn[] { dateColumn, weekdayColumn, role1Column, role2Column, role3Column, role4Column, issueColumn });
        scheduleGrid.Dock = DockStyle.Fill;
        scheduleGrid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
        scheduleGrid.Location = new Point(3, 61);
        scheduleGrid.MultiSelect = false;
        scheduleGrid.Name = "scheduleGrid";
        scheduleGrid.RowHeadersWidth = 48;
        scheduleGrid.RowTemplate.Height = 32;
        scheduleGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        scheduleGrid.Size = new Size(966, 643);
        scheduleGrid.TabIndex = 1;
        scheduleGrid.CellEndEdit += scheduleGrid_CellEndEdit;
        scheduleGrid.CellFormatting += scheduleGrid_CellFormatting;
        scheduleGrid.DataError += scheduleGrid_DataError;
        // 
        // dateColumn
        // 
        dateColumn.DataPropertyName = "DateText";
        dateColumn.FillWeight = 70F;
        dateColumn.HeaderText = "日期";
        dateColumn.MinimumWidth = 72;
        dateColumn.Name = "dateColumn";
        // 
        // weekdayColumn
        // 
        weekdayColumn.DataPropertyName = "Weekday";
        weekdayColumn.FillWeight = 78F;
        weekdayColumn.HeaderText = "星期";
        weekdayColumn.MinimumWidth = 78;
        weekdayColumn.Name = "weekdayColumn";
        // 
        // role1Column
        // 
        role1Column.DataPropertyName = "Role1";
        role1Column.HeaderText = "岗位一（队长）";
        role1Column.MinimumWidth = 115;
        role1Column.Name = "role1Column";
        // 
        // role2Column
        // 
        role2Column.DataPropertyName = "Role2";
        role2Column.HeaderText = "岗位二";
        role2Column.MinimumWidth = 90;
        role2Column.Name = "role2Column";
        // 
        // role3Column
        // 
        role3Column.DataPropertyName = "Role3";
        role3Column.HeaderText = "岗位三";
        role3Column.MinimumWidth = 90;
        role3Column.Name = "role3Column";
        // 
        // role4Column
        // 
        role4Column.DataPropertyName = "Role4";
        role4Column.HeaderText = "岗位四";
        role4Column.MinimumWidth = 90;
        role4Column.Name = "role4Column";
        // 
        // issueColumn
        // 
        issueColumn.DataPropertyName = "IssueText";
        issueColumn.FillWeight = 160F;
        issueColumn.HeaderText = "异常 / 待检查";
        issueColumn.MinimumWidth = 180;
        issueColumn.Name = "issueColumn";
        issueColumn.ReadOnly = true;
        // 
        // sourceTabPage
        // 
        sourceTabPage.Controls.Add(sourceSplit);
        sourceTabPage.Location = new Point(4, 29);
        sourceTabPage.Name = "sourceTabPage";
        sourceTabPage.Padding = new Padding(8);
        sourceTabPage.Size = new Size(988, 723);
        sourceTabPage.TabIndex = 1;
        sourceTabPage.Text = "原图与识别诊断";
        sourceTabPage.UseVisualStyleBackColor = true;
        // 
        // sourceSplit
        // 
        sourceSplit.Dock = DockStyle.Fill;
        sourceSplit.Location = new Point(8, 8);
        sourceSplit.Name = "sourceSplit";
        sourceSplit.Orientation = Orientation.Horizontal;
        // 
        // sourceSplit.Panel1
        // 
        sourceSplit.Panel1.Controls.Add(previewPictureBox);
        sourceSplit.Panel1MinSize = 260;
        // 
        // sourceSplit.Panel2
        // 
        sourceSplit.Panel2.Controls.Add(diagnosticsTextBox);
        sourceSplit.Panel2MinSize = 110;
        sourceSplit.Size = new Size(972, 707);
        sourceSplit.SplitterDistance = 493;
        sourceSplit.TabIndex = 0;
        // 
        // previewPictureBox
        // 
        previewPictureBox.BackColor = Color.FromArgb(35, 35, 35);
        previewPictureBox.Dock = DockStyle.Fill;
        previewPictureBox.Location = new Point(0, 0);
        previewPictureBox.Name = "previewPictureBox";
        previewPictureBox.Size = new Size(972, 493);
        previewPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        previewPictureBox.TabIndex = 0;
        previewPictureBox.TabStop = false;
        // 
        // diagnosticsTextBox
        // 
        diagnosticsTextBox.BackColor = Color.White;
        diagnosticsTextBox.Dock = DockStyle.Fill;
        diagnosticsTextBox.Font = new Font("Consolas", 9F);
        diagnosticsTextBox.Location = new Point(0, 0);
        diagnosticsTextBox.Multiline = true;
        diagnosticsTextBox.Name = "diagnosticsTextBox";
        diagnosticsTextBox.ReadOnly = true;
        diagnosticsTextBox.ScrollBars = ScrollBars.Both;
        diagnosticsTextBox.Size = new Size(972, 210);
        diagnosticsTextBox.TabIndex = 0;
        diagnosticsTextBox.WordWrap = false;
        // 
        // statusStrip
        // 
        statusStrip.Items.AddRange(new ToolStripItem[] { progressBar, statusLabel });
        statusStrip.Location = new Point(0, 834);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(1380, 26);
        statusStrip.SizingGrip = false;
        statusStrip.TabIndex = 2;
        // 
        // progressBar
        // 
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(160, 18);
        progressBar.Visible = false;
        // 
        // statusLabel
        // 
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(69, 20);
        statusLabel.Text = "准备就绪";
        // 
        // FormOcr
        // 
        AutoScaleDimensions = new SizeF(9F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1380, 860);
        Controls.Add(rootLayout);
        Font = new Font("Microsoft YaHei UI", 9F);
        MinimumSize = new Size(1120, 720);
        Name = "FormOcr";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "本地排班图片整理 - RapidOCR";
        FormClosing += FormOcr_FormClosing;
        Load += FormOcr_Load;
        rootLayout.ResumeLayout(false);
        rootLayout.PerformLayout();
        headerPanel.ResumeLayout(false);
        headerPanel.PerformLayout();
        mainSplit.Panel1.ResumeLayout(false);
        mainSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)mainSplit).EndInit();
        mainSplit.ResumeLayout(false);
        leftPanel.ResumeLayout(false);
        modelGroupBox.ResumeLayout(false);
        modelGroupBox.PerformLayout();
        settingsGroupBox.ResumeLayout(false);
        settingsGroupBox.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)role4ColumnNumeric).EndInit();
        ((System.ComponentModel.ISupportInitialize)role3ColumnNumeric).EndInit();
        imagesGroupBox.ResumeLayout(false);
        outputTabs.ResumeLayout(false);
        resultTabPage.ResumeLayout(false);
        resultLayout.ResumeLayout(false);
        resultToolbarPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)scheduleGrid).EndInit();
        sourceTabPage.ResumeLayout(false);
        sourceSplit.Panel1.ResumeLayout(false);
        sourceSplit.Panel2.ResumeLayout(false);
        sourceSplit.Panel2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)sourceSplit).EndInit();
        sourceSplit.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)previewPictureBox).EndInit();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel rootLayout;
    private Panel headerPanel;
    private Label modelStateLabel;
    private Label subtitleLabel;
    private Label titleLabel;
    private SplitContainer mainSplit;
    private Panel leftPanel;
    private GroupBox imagesGroupBox;
    private ListView imageListView;
    private ColumnHeader imageNumberColumn;
    private ColumnHeader imageNameColumn;
    private ColumnHeader imageStatusColumn;
    private Button selectImagesButton;
    private Button clearImagesButton;
    private GroupBox settingsGroupBox;
    private DateTimePicker monthPicker;
    private Label targetMonthLabel;
    private NumericUpDown role3ColumnNumeric;
    private Label role3ColumnLabel;
    private NumericUpDown role4ColumnNumeric;
    private Label role4ColumnLabel;
    private Label layoutHintLabel;
    private GroupBox modelGroupBox;
    private TextBox modelPathTextBox;
    private Label modelPathLabel;
    private Button browseModelButton;
    private Button initializeModelButton;
    private Label modelStatusLabel;
    private Button processButton;
    private TabControl outputTabs;
    private TabPage resultTabPage;
    private TableLayoutPanel resultLayout;
    private Panel resultToolbarPanel;
    private Label resultSummaryLabel;
    private Button revalidateButton;
    private Button exportButton;
    private DataGridView scheduleGrid;
    private DataGridViewTextBoxColumn dateColumn;
    private DataGridViewTextBoxColumn weekdayColumn;
    private DataGridViewTextBoxColumn role1Column;
    private DataGridViewTextBoxColumn role2Column;
    private DataGridViewTextBoxColumn role3Column;
    private DataGridViewTextBoxColumn role4Column;
    private DataGridViewTextBoxColumn issueColumn;
    private TabPage sourceTabPage;
    private SplitContainer sourceSplit;
    private PictureBox previewPictureBox;
    private TextBox diagnosticsTextBox;
    private StatusStrip statusStrip;
    private ToolStripProgressBar progressBar;
    private ToolStripStatusLabel statusLabel;
}
