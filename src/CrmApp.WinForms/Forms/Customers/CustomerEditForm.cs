using CrmApp.Core.Abstractions;
using CrmApp.Core.Enums;
using CrmApp.Core.Models;
using CrmApp.Core.Validation;
using CrmApp.WinForms.Localization;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace CrmApp.WinForms.Forms.Customers;

public partial class CustomerEditForm : Form
{
    private readonly ICustomerRepository _repository;
    private readonly CustomerValidator _validator;
    private readonly ILogger<CustomerEditForm> _logger;

    private Customer _customer = new();
    private bool _isNew = true;

    public CustomerEditForm(
        ICustomerRepository repository,
        CustomerValidator validator,
        ILogger<CustomerEditForm> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _validator = validator;
        _logger = logger;

        InitializeComponent();
        FillCombos();

        AcceptButton = _saveButton;
        CancelButton = _cancelButton;

        _typeCombo.SelectedIndexChanged += (_, _) => UpdateTypeSpecificFields();

        // ИНН — только цифры. MaxLength уже ограничивает 12 символами,
        // а здесь блокируем нечисловые символы при вводе.
        _innTextBox.KeyPress += (_, e) =>
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        };
    }

    private void FillCombos()
    {
        _typeCombo.Items.Clear();
        foreach (var t in Enum.GetValues<CustomerType>())
        {
            _typeCombo.Items.Add(t);
        }
        // Отрисовка значений в выпадающем списке по-русски (по запросу пользователя).
        // Сами Items хранят CustomerType — фильтрация и BindToModel продолжают работать с enum.
        _typeCombo.Format += (_, e) =>
        {
            if (e.ListItem is CustomerType t) e.Value = t.ToRussian();
        };

        _statusCombo.Items.Clear();
        foreach (var s in Enum.GetValues<CustomerStatus>())
        {
            _statusCombo.Items.Add(s);
        }
        _statusCombo.Format += (_, e) =>
        {
            if (e.ListItem is CustomerStatus s) e.Value = s.ToRussian();
        };
    }

    public void SetCustomer(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        _customer = customer;
        _isNew = customer.CreatedAt == default;
        Text = _isNew ? "Новый клиент" : $"Клиент: {customer.DisplayName}";
        BindFromModel();
        UpdateTypeSpecificFields();
    }

    private void BindFromModel()
    {
        _typeCombo.SelectedItem = _customer.Type;
        _statusCombo.SelectedItem = _customer.Status;
        _nameTextBox.Text = _customer.Name;
        _phoneTextBox.Text = _customer.Phone ?? string.Empty;
        _emailTextBox.Text = _customer.Email ?? string.Empty;
        _addressTextBox.Text = _customer.Address ?? string.Empty;
        _notesTextBox.Text = _customer.Notes ?? string.Empty;
        _companyNameTextBox.Text = _customer.CompanyName ?? string.Empty;
        _innTextBox.Text = _customer.Inn ?? string.Empty;
        _positionTextBox.Text = _customer.Position ?? string.Empty;

        if (_customer.BirthDate.HasValue)
        {
            _hasBirthDateCheck.Checked = true;
            _birthDatePicker.Value = _customer.BirthDate.Value.ToDateTime(TimeOnly.MinValue);
        }
        else
        {
            _hasBirthDateCheck.Checked = false;
        }
    }

    private void BindToModel()
    {
        _customer.Type = (CustomerType)_typeCombo.SelectedItem!;
        _customer.Status = (CustomerStatus)_statusCombo.SelectedItem!;
        _customer.Name = _nameTextBox.Text.Trim();
        // MaskedTextBox: считаем телефон валидным только если маска заполнена целиком.
        // Иначе — null (пустое значение), чтобы пользователь не сохранял "+7 (495) ___-__-__".
        _customer.Phone = _phoneTextBox.MaskCompleted ? _phoneTextBox.Text : null;
        _customer.Email = NullIfBlank(_emailTextBox.Text);
        _customer.Address = NullIfBlank(_addressTextBox.Text);
        _customer.Notes = NullIfBlank(_notesTextBox.Text);

        if (_customer.Type == CustomerType.Company)
        {
            _customer.CompanyName = NullIfBlank(_companyNameTextBox.Text);
            _customer.Inn = NullIfBlank(_innTextBox.Text);
            _customer.Position = NullIfBlank(_positionTextBox.Text);
            _customer.BirthDate = null;
        }
        else
        {
            _customer.CompanyName = null;
            _customer.Inn = null;
            _customer.Position = null;
            _customer.BirthDate = _hasBirthDateCheck.Checked
                ? DateOnly.FromDateTime(_birthDatePicker.Value)
                : null;
        }
    }

    private static string? NullIfBlank(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private void UpdateTypeSpecificFields()
    {
        var isCompany = (_typeCombo.SelectedItem as CustomerType?) == CustomerType.Company;
        _companyGroup.Visible = isCompany;
        _personGroup.Visible = !isCompany;
    }

    private async void OnSaveClick(object? sender, EventArgs e)
    {
        BindToModel();

        var result = await _validator.ValidateAsync(_customer);
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
                await _repository.AddAsync(_customer);
            }
            else
            {
                await _repository.UpdateAsync(_customer);
            }
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сохранения клиента");
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
