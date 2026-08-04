using RapidOCRSchedule.Core;

namespace RapidOCRWinform;

internal sealed class EmployeeRosterDialog : Form
{
    private readonly TextBox _namesTextBox = new();
    private readonly Label _countLabel = new();
    private readonly bool _convertTraditionalToSimplified;

    public EmployeeRosterDialog(
        IEnumerable<string> employeeNames,
        bool convertTraditionalToSimplified)
    {
        _convertTraditionalToSimplified = convertTraditionalToSimplified;
        EmployeeNames = EmployeeNameResolver.NormalizeEmployeeNames(employeeNames, convertTraditionalToSimplified);
        InitializeDialog();
        _namesTextBox.Lines = EmployeeNames.ToArray();
        UpdateCount();
    }

    public IReadOnlyList<string> EmployeeNames { get; private set; }

    private void InitializeDialog()
    {
        Text = "员工名单配置";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 520);
        MinimumSize = new Size(440, 400);
        Font = new Font("Microsoft YaHei UI", 9F);
        ShowIcon = false;
        ShowInTaskbar = false;

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 4,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Label instructions = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10),
            Text = _convertTraditionalToSimplified
                ? "每行填写一个员工姓名，当前会自动保存为简体。\n识别结果只有在名单中存在唯一匹配时才会自动纠正。"
                : "每行填写一个员工姓名，当前会保留输入字形。\n识别结果只有在名单中存在唯一匹配时才会自动纠正。",
        };

        _namesTextBox.AcceptsReturn = true;
        _namesTextBox.AcceptsTab = false;
        _namesTextBox.Dock = DockStyle.Fill;
        _namesTextBox.Multiline = true;
        _namesTextBox.ScrollBars = ScrollBars.Vertical;
        _namesTextBox.WordWrap = false;
        _namesTextBox.Font = new Font("Microsoft YaHei UI", 11F);
        _namesTextBox.TextChanged += (_, _) => UpdateCount();

        _countLabel.AutoSize = true;
        _countLabel.Dock = DockStyle.Fill;
        _countLabel.ForeColor = Color.DimGray;
        _countLabel.Margin = new Padding(0, 8, 0, 8);

        FlowLayoutPanel buttons = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = Padding.Empty,
        };

        Button saveButton = new()
        {
            Text = "保存",
            Size = new Size(104, 36),
            Margin = new Padding(8, 0, 0, 0),
        };
        saveButton.Click += SaveButton_Click;

        Button cancelButton = new()
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            Size = new Size(104, 36),
            Margin = Padding.Empty,
        };

        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(cancelButton);
        layout.Controls.Add(instructions, 0, 0);
        layout.Controls.Add(_namesTextBox, 0, 1);
        layout.Controls.Add(_countLabel, 0, 2);
        layout.Controls.Add(buttons, 0, 3);
        Controls.Add(layout);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        EmployeeNames = ParseEmployeeNames();
        DialogResult = DialogResult.OK;
        Close();
    }

    private void UpdateCount()
    {
        int rawCount = _namesTextBox.Lines.Count(static line => !string.IsNullOrWhiteSpace(line));
        int normalizedCount = ParseEmployeeNames().Count;
        _countLabel.Text = rawCount == normalizedCount
            ? $"共 {normalizedCount} 人；保存后关闭软件仍会保留。"
            : $"有效姓名 {normalizedCount} 人；已自动忽略空行或重复姓名。";
    }

    private IReadOnlyList<string> ParseEmployeeNames() =>
        EmployeeNameResolver.NormalizeEmployeeNames(_namesTextBox.Lines, _convertTraditionalToSimplified);
}
