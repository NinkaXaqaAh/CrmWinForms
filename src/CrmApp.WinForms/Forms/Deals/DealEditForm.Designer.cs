#nullable enable

using CrmApp.WinForms.Theming;

namespace CrmApp.WinForms.Forms.Deals;

partial class DealEditForm
{
    private System.ComponentModel.IContainer? components = null;

    private TableLayoutPanel _root = null!;

    private TextBox _titleTextBox = null!;
    private TextBox _descriptionTextBox = null!;
    private ComboBox _stageCombo = null!;
    private ComboBox _customerCombo = null!;
    private NumericUpDown _amountNumeric = null!;
    private ComboBox _currencyCombo = null!;
    private NumericUpDown _probabilityNumeric = null!;
    private CheckBox _hasExpectedDateCheck = null!;
    private DateTimePicker _expectedDatePicker = null!;

    private Label _actualDateLabel = null!;
    private DateTimePicker _actualDatePicker = null!;

    private Panel _buttonsPanel = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        _titleTextBox = MakeTextBox();
        _descriptionTextBox = MakeTextBox(multiline: true);

        _stageCombo = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FormattingEnabled = true,
            Font = new Font("Segoe UI", 10F),
        };

        _customerCombo = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FormattingEnabled = true,
            Font = new Font("Segoe UI", 10F),
        };

        _amountNumeric = new NumericUpDown
        {
            Maximum = 999_999_999_999m,
            Minimum = 0m,
            DecimalPlaces = 2,
            ThousandsSeparator = true,
            Width = 200,
            Font = new Font("Segoe UI", 10F),
        };
        _currencyCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 100,
            Font = new Font("Segoe UI", 10F),
        };
        var amountPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0),
        };
        amountPanel.Controls.Add(_amountNumeric);
        amountPanel.Controls.Add(_currencyCombo);

        _probabilityNumeric = new NumericUpDown
        {
            Minimum = 0, Maximum = 100, Width = 100,
            Font = new Font("Segoe UI", 10F),
        };

        _hasExpectedDateCheck = new CheckBox
        {
            Text = "Дата задана",
            AutoSize = true,
            Margin = new Padding(0, 6, 8, 0),
            ForeColor = AppPalette.TextPrimary,
        };
        _expectedDatePicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Width = 200,
        };
        var expectedPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0),
        };
        expectedPanel.Controls.Add(_hasExpectedDateCheck);
        expectedPanel.Controls.Add(_expectedDatePicker);

        _actualDateLabel = MakeLabel("Дата фактического закрытия");
        _actualDatePicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Width = 200,
        };

        _saveButton = new Button
        {
            Text = "Сохранить",
            Size = new Size(140, 36),
            BackColor = AppPalette.Accent,
            ForeColor = AppPalette.AccentText,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.5F),
        };
        _saveButton.FlatAppearance.BorderSize = 0;
        _saveButton.Click += OnSaveClick;

        _cancelButton = new Button
        {
            Text = "Отмена",
            Size = new Size(120, 36),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5F),
            BackColor = AppPalette.Surface,
            ForeColor = AppPalette.TextPrimary,
        };
        _cancelButton.FlatAppearance.BorderColor = AppPalette.BorderMuted;
        _cancelButton.Click += OnCancelClick;

        _buttonsPanel = new Panel { Dock = DockStyle.Fill, Height = 50 };
        _buttonsPanel.Controls.Add(_saveButton);
        _buttonsPanel.Controls.Add(_cancelButton);
        _buttonsPanel.Resize += (_, _) =>
        {
            _saveButton.Location = new Point(_buttonsPanel.Width - _saveButton.Width, 8);
            _cancelButton.Location = new Point(_buttonsPanel.Width - _saveButton.Width - _cancelButton.Width - 8, 8);
        };

        _root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(20),
            BackColor = AppPalette.Surface, AutoScroll = true,
        };
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(MakeLabel("Название"), _titleTextBox);
        AddRow(MakeLabel("Описание"), _descriptionTextBox);
        AddRow(MakeLabel("Этап"), _stageCombo);
        AddRow(MakeLabel("Клиент"), _customerCombo);
        AddRow(MakeLabel("Сумма"), amountPanel);
        AddRow(MakeLabel("Вероятность, %"), _probabilityNumeric);
        AddRow(MakeLabel("Ожидаемое закрытие"), expectedPanel);
        AddRow(_actualDateLabel, _actualDatePicker);
        AddRow(_buttonsPanel, columnSpan: 2);

        Controls.Add(_root);

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(600, 520);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Сделка";
        Font = new Font("Segoe UI", 9F);
        BackColor = AppPalette.Surface;
        ForeColor = AppPalette.TextPrimary;

        ResumeLayout(false);
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Margin = new Padding(0, 8, 10, 0),
        ForeColor = AppPalette.TextSecondary,
    };

    private static TextBox MakeTextBox(bool multiline = false) => new()
    {
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 10F),
        Multiline = multiline,
        Height = multiline ? 80 : 25,
        ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None,
    };

    private void AddRow(Control labelOrSpan, Control? right = null, int columnSpan = 1)
    {
        if (right is null)
        {
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.SetColumnSpan(labelOrSpan, columnSpan);
            _root.Controls.Add(labelOrSpan, 0, _root.RowCount);
            _root.RowCount++;
        }
        else
        {
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.Controls.Add(labelOrSpan, 0, _root.RowCount);
            _root.Controls.Add(right, 1, _root.RowCount);
            _root.RowCount++;
        }
    }
}
