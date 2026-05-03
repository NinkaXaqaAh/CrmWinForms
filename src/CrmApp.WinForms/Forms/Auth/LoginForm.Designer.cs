#nullable enable

using CrmApp.WinForms.Theming;

namespace CrmApp.WinForms.Forms.Auth;

partial class LoginForm
{
    private System.ComponentModel.IContainer? components = null;

    private Label _titleLabel = null!;
    private Label _loginLabel = null!;
    private Label _passwordLabel = null!;
    private TextBox _loginTextBox = null!;
    private TextBox _passwordTextBox = null!;
    private Button _loginButton = null!;
    private Button _cancelButton = null!;
    private Label _statusLabel = null!;
    private Label _hintLabel = null!;

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

        _titleLabel = new Label
        {
            Text = "Вход в CRM",
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            ForeColor = AppPalette.TextPrimary,
            Location = new Point(40, 30),
            AutoSize = true,
        };

        _loginLabel = new Label
        {
            Text = "Логин",
            Location = new Point(40, 90),
            AutoSize = true,
            ForeColor = AppPalette.TextPrimary,
        };

        _loginTextBox = new TextBox
        {
            Location = new Point(40, 112),
            Size = new Size(340, 25),
            Font = new Font("Segoe UI", 10F),
        };

        _passwordLabel = new Label
        {
            Text = "Пароль",
            Location = new Point(40, 152),
            AutoSize = true,
            ForeColor = AppPalette.TextPrimary,
        };

        _passwordTextBox = new TextBox
        {
            Location = new Point(40, 174),
            Size = new Size(340, 25),
            Font = new Font("Segoe UI", 10F),
            UseSystemPasswordChar = true,
        };

        _statusLabel = new Label
        {
            Location = new Point(40, 210),
            Size = new Size(340, 36),
            ForeColor = AppPalette.Danger,
            Text = string.Empty,
        };

        _loginButton = new Button
        {
            Text = "Войти",
            Location = new Point(220, 250),
            Size = new Size(160, 36),
            Font = new Font("Segoe UI Semibold", 10F),
            BackColor = AppPalette.Accent,
            ForeColor = AppPalette.AccentText,
            FlatStyle = FlatStyle.Flat,
        };
        _loginButton.FlatAppearance.BorderSize = 0;
        _loginButton.Click += OnLoginClick;

        _cancelButton = new Button
        {
            Text = "Отмена",
            Location = new Point(40, 250),
            Size = new Size(160, 36),
            Font = new Font("Segoe UI", 10F),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppPalette.Surface,
            ForeColor = AppPalette.TextPrimary,
        };
        _cancelButton.FlatAppearance.BorderColor = AppPalette.BorderMuted;
        _cancelButton.Click += OnCancelClick;

        _hintLabel = new Label
        {
            Location = new Point(40, 305),
            Size = new Size(340, 30),
            ForeColor = AppPalette.TextDisabled,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
            TextAlign = ContentAlignment.MiddleCenter,
        };

        Controls.AddRange(new Control[]
        {
            _titleLabel, _loginLabel, _loginTextBox,
            _passwordLabel, _passwordTextBox, _statusLabel,
            _loginButton, _cancelButton, _hintLabel
        });

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(420, 350);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Вход в CRM";
        BackColor = AppPalette.Surface;
        ForeColor = AppPalette.TextPrimary;
        Font = new Font("Segoe UI", 9F);

        ResumeLayout(false);
        PerformLayout();
    }
}
