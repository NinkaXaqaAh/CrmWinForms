using CrmApp.Core.Abstractions;

namespace CrmApp.WinForms.Forms.Auth;

public partial class LoginForm : Form
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserContext _userContext;

    public LoginForm(IAuthService authService, ICurrentUserContext userContext)
    {
        ArgumentNullException.ThrowIfNull(authService);
        ArgumentNullException.ThrowIfNull(userContext);

        _authService = authService;
        _userContext = userContext;

        InitializeComponent();

        // Подсказка про seed-пользователей в подножии формы.
        // На Этапе 4/5 это уберём за галку "показать подсказку".
        _hintLabel.Text = "Демо-вход:  admin / admin   или   manager / manager";

        AcceptButton = _loginButton;
        CancelButton = _cancelButton;
        _loginTextBox.Focus();
    }

    private async void OnLoginClick(object? sender, EventArgs e)
    {
        var login = _loginTextBox.Text.Trim();
        var password = _passwordTextBox.Text;

        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            ShowError("Введите логин и пароль");
            return;
        }

        // Блокируем UI на время авторизации - чтобы избежать двойного клика.
        SetBusy(true);
        try
        {
            var user = await _authService.AuthenticateAsync(login, password);
            if (user is null)
            {
                ShowError("Неверный логин или пароль");
                _passwordTextBox.SelectAll();
                _passwordTextBox.Focus();
                return;
            }

            _userContext.SetCurrent(user);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка входа: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void OnCancelClick(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void SetBusy(bool busy)
    {
        _loginTextBox.Enabled = !busy;
        _passwordTextBox.Enabled = !busy;
        _loginButton.Enabled = !busy;
        _cancelButton.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        _statusLabel.Text = busy ? "Проверяем..." : string.Empty;
    }

    private void ShowError(string message)
    {
        _statusLabel.ForeColor = Color.FromArgb(220, 53, 69);
        _statusLabel.Text = message;
    }
}
