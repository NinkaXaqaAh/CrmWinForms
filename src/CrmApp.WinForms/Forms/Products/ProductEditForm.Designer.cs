#nullable enable

using CrmApp.WinForms.Theming;

namespace CrmApp.WinForms.Forms.Products;

partial class ProductEditForm
{
    private System.ComponentModel.IContainer? components = null;

    private TableLayoutPanel _root = null!;

    private TextBox _nameTextBox = null!;
    private TextBox _skuTextBox = null!;
    private TextBox _categoryTextBox = null!;
    private TextBox _descriptionTextBox = null!;
    private NumericUpDown _priceNumeric = null!;
    private ComboBox _currencyCombo = null!;
    private CheckBox _isActiveCheck = null!;

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

        _nameTextBox = MakeTextBox();
        _skuTextBox = MakeTextBox();
        _categoryTextBox = MakeTextBox();
        _descriptionTextBox = MakeTextBox(multiline: true);

        _priceNumeric = new NumericUpDown
        {
            Minimum = 0m,
            Maximum = 999_999_999m,
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

        var pricePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(0),
        };
        pricePanel.Controls.Add(_priceNumeric);
        pricePanel.Controls.Add(_currencyCombo);

        _isActiveCheck = new CheckBox
        {
            Text = "Активен (доступен для продажи)",
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = AppPalette.TextPrimary,
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
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(MakeLabel("Название"), _nameTextBox);
        AddRow(MakeLabel("Артикул (SKU)"), _skuTextBox);
        AddRow(MakeLabel("Категория"), _categoryTextBox);
        AddRow(MakeLabel("Описание"), _descriptionTextBox);
        AddRow(MakeLabel("Цена"), pricePanel);
        AddRow(new Label { AutoSize = true }, _isActiveCheck);
        AddRow(_buttonsPanel, columnSpan: 2);

        Controls.Add(_root);

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(540, 420);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Товар";
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
