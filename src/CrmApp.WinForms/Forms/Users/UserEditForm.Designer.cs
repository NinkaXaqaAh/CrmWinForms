#nullable enable

using CrmApp.WinForms.Theming;

namespace CrmApp.WinForms.Forms.Users;

partial class UserEditForm
{
    private System.ComponentModel.IContainer? components = null;

    private TableLayoutPanel _root = null!;

    private TextBox _loginTextBox = null!;
    private TextBox _fullNameTextBox = null!;
    private TextBox _emailTextBox = null!;
    private ComboBox _roleCombo = null!;
    private CheckBox _isActiveCheck = null!;
    private TextBox _passwordTextBox = null!;
    private TextBox _passwordConfirmTextBox = null!;
    private Label _passwordHintLabel = null!;

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

        _loginTextBox = MakeTextBox();
        _loginTextBox.MaxLength = 50;
        _fullNameTextBox = MakeTextBox();
        _fullNameTextBox.MaxLength = 200;
        _emailTextBox = MakeTextBox();
        _emailTextBox.MaxLength = 200;

        _roleCombo = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FormattingEnabled = true,
            Font = new Font("Segoe UI", 10F),
        };

        _isActiveCheck = new CheckBox
        {
            Text = "Активен (может входить в систему)",
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F),
            Checked = true,
            ForeColor = AppPalette.TextPrimary,
        };

        _passwordTextBox = MakeTextBox();
        _passwordTextBox.UseSystemPasswordChar = true;
        _passwordTextBox.MaxLength = 100;

        _passwordConfirmTextBox = MakeTextBox();
        _passwordConfirmTextBox.UseSystemPasswordChar = true;
        _passwordConfirmTextBox.MaxLength = 100;

        _passwordHintLabel = new Label
        {
            AutoSize = true,
            ForeColor = AppPalette.Muted,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
            Margin = new Padding(0, 4, 0, 0),
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

        AddRow(MakeLabel("Логин"), _loginTextBox);
        AddRow(MakeLabel("ФИО"), _fullNameTextBox);
        AddRow(MakeLabel("Email"), _emailTextBox);
        AddRow(MakeLabel("Роль"), _roleCombo);
        AddRow(new Label { AutoSize = true }, _isActiveCheck);
        AddRow(MakeLabel("Пароль"), _passwordTextBox);
        AddRow(MakeLabel("Подтверждение"), _passwordConfirmTextBox);
        AddRow(new Label { AutoSize = true }, _passwordHintLabel);
        AddRow(_buttonsPanel, columnSpan: 2);

        Controls.Add(_root);

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(620, 480);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Пользователь";
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

    private static TextBox MakeTextBox() => new()
    {
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 10F),
        Height = 25,
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
