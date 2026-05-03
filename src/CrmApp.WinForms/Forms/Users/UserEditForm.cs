using CrmApp.Core.Abstractions;
using CrmApp.Core.Enums;
using CrmApp.Core.Models;
using CrmApp.Core.Validation;
using CrmApp.WinForms.Localization;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace CrmApp.WinForms.Forms.Users;

public partial class UserEditForm : Form
{
    private readonly IUserRepository _users;
    private readonly IAuthService _auth;
    private readonly UserValidator _validator;
    private readonly ILogger<UserEditForm> _logger;

    private User _user = new();
    private bool _isNew = true;

    private const int MinPasswordLength = 4;

    public UserEditForm(
        IUserRepository users,
        IAuthService auth,
        UserValidator validator,
        ILogger<UserEditForm> logger)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(auth);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(logger);

        _users = users;
        _auth = auth;
        _validator = validator;
        _logger = logger;

        InitializeComponent();
        FillCombos();

        AcceptButton = _saveButton;
        CancelButton = _cancelButton;
    }

    private void FillCombos()
    {
        _roleCombo.Items.Clear();
        foreach (var r in Enum.GetValues<UserRole>()) _roleCombo.Items.Add(r);
        _roleCombo.Format += (_, e) =>
        {
            if (e.ListItem is UserRole r) e.Value = r.ToRussian();
        };
    }

    public void SetUser(User user, bool isNew)
    {
        ArgumentNullException.ThrowIfNull(user);
        _user = user;
        _isNew = isNew;

        Text = _isNew ? "Новый пользователь" : $"Пользователь: {user.Login}";

        // Подсказка под паролем меняется в зависимости от режима.
        _passwordHintLabel.Text = _isNew
            ? $"Минимум {MinPasswordLength} символа."
            : $"Оставьте пустым, чтобы не менять пароль. Минимум {MinPasswordLength} символа.";

        BindFromModel();
    }

    private void BindFromModel()
    {
        _loginTextBox.Text = _user.Login;
        _fullNameTextBox.Text = _user.FullName;
        _emailTextBox.Text = _user.Email ?? string.Empty;
        _roleCombo.SelectedItem = _user.Role;
        _isActiveCheck.Checked = _isNew || _user.IsActive;
        _passwordTextBox.Text = string.Empty;
        _passwordConfirmTextBox.Text = string.Empty;
    }

    private void BindToModel()
    {
        _user.Login = _loginTextBox.Text.Trim();
        _user.FullName = _fullNameTextBox.Text.Trim();
        _user.Email = string.IsNullOrWhiteSpace(_emailTextBox.Text) ? null : _emailTextBox.Text.Trim();
        _user.Role = (UserRole)_roleCombo.SelectedItem!;
        _user.IsActive = _isActiveCheck.Checked;
    }

    private async void OnSaveClick(object? sender, EventArgs e)
    {
        BindToModel();

        // Логика пароля:
        //   - новый пользователь: пароль обязателен;
        //   - редактирование: пустые поля = не менять текущий хеш;
        //   - если введён пароль, должен совпадать с подтверждением и иметь минимум N символов.
        var pwd = _passwordTextBox.Text;
        var pwdConfirm = _passwordConfirmTextBox.Text;
        var pwdProvided = !string.IsNullOrEmpty(pwd) || !string.IsNullOrEmpty(pwdConfirm);

        if (_isNew && !pwdProvided)
        {
            MessageBox.Show("Для нового пользователя задайте пароль.",
                "Проверьте поля", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (pwdProvided)
        {
            if (pwd != pwdConfirm)
            {
                MessageBox.Show("Пароль и подтверждение не совпадают.",
                    "Проверьте поля", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (pwd.Length < MinPasswordLength)
            {
                MessageBox.Show($"Пароль должен быть минимум {MinPasswordLength} символа.",
                    "Проверьте поля", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _user.PasswordHash = _auth.HashPassword(pwd);
        }
        // Если pwdProvided=false и !_isNew — PasswordHash остаётся прежним из Clone'а в ListForm.

        var result = await _validator.ValidateAsync(_user);
        if (!result.IsValid)
        {
            var msg = string.Join(Environment.NewLine,
                result.Errors.Select(err => "• " + err.ErrorMessage));
            MessageBox.Show(msg, "Проверьте поля",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            _saveButton.Enabled = false;
            if (_isNew)
            {
                // Дополнительная проверка уникальности логина перед AddAsync —
                // ловим конфликт раньше, чем JsonRepository выдаст InvalidOperationException по Id.
                var existing = await _users.FindByLoginAsync(_user.Login);
                if (existing is not null)
                {
                    MessageBox.Show($"Пользователь с логином \"{_user.Login}\" уже существует.",
                        "Конфликт логина", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _saveButton.Enabled = true;
                    return;
                }
                await _users.AddAsync(_user);
            }
            else
            {
                await _users.UpdateAsync(_user);
            }
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сохранения пользователя");
            MessageBox.Show("Не удалось сохранить: " + ex.Message,
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _saveButton.Enabled = true;
        }
    }

    private void OnCancelClick(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
