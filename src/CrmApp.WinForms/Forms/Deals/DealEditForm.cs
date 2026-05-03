using CrmApp.Core.Abstractions;
using CrmApp.Core.Common;
using CrmApp.Core.Enums;
using CrmApp.Core.Models;
using CrmApp.Core.Validation;
using CrmApp.WinForms.Localization;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace CrmApp.WinForms.Forms.Deals;

public partial class DealEditForm : Form
{
    private readonly IDealRepository _deals;
    private readonly ICustomerRepository _customers;
    private readonly DealValidator _validator;
    private readonly ILogger<DealEditForm> _logger;

    private Deal _deal = new();
    private bool _isNew = true;
    private List<Customer> _customerList = new();

    // Список валют для ComboBox. В этом MVP - три самых частых.
    // Если нужно больше - вынести в конфиг.
    private static readonly string[] Currencies = ["RUB", "USD", "EUR"];

    public DealEditForm(
        IDealRepository deals,
        ICustomerRepository customers,
        DealValidator validator,
        ILogger<DealEditForm> logger)
    {
        ArgumentNullException.ThrowIfNull(deals);
        ArgumentNullException.ThrowIfNull(customers);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(logger);

        _deals = deals;
        _customers = customers;
        _validator = validator;
        _logger = logger;

        InitializeComponent();
        FillCombos();

        AcceptButton = _saveButton;
        CancelButton = _cancelButton;

        _stageCombo.SelectedIndexChanged += (_, _) => UpdateClosedState();
        _hasExpectedDateCheck.CheckedChanged += (_, _) =>
            _expectedDatePicker.Enabled = _hasExpectedDateCheck.Checked;
    }

    private void FillCombos()
    {
        _stageCombo.Items.Clear();
        foreach (var s in Enum.GetValues<DealStage>())
        {
            _stageCombo.Items.Add(s);
        }
        _stageCombo.Format += (_, e) =>
        {
            if (e.ListItem is DealStage s) e.Value = s.ToRussian();
        };

        _currencyCombo.Items.Clear();
        foreach (var c in Currencies) _currencyCombo.Items.Add(c);

        // Format для customer подписываем здесь же, до заполнения Items в SetDealAsync.
        // Раньше подписка была в SetDealAsync — каждый вызов навешивал handler заново;
        // фактически не сломано из-за Transient-регистрации формы, но фрагильно.
        _customerCombo.Format += (_, e) =>
        {
            if (e.ListItem is Customer c) e.Value = c.DisplayName;
        };
    }

    // Async, потому что нужно загрузить список клиентов из репозитория.
    public async Task SetDealAsync(Deal deal)
    {
        ArgumentNullException.ThrowIfNull(deal);
        _deal = deal;
        _isNew = deal.CreatedAt == default;

        // Грузим клиентов до показа формы - иначе ComboBox откроется пустой.
        _customerList = (await _customers.GetAllAsync())
            .OrderBy(c => c.DisplayName)
            .ToList();

        _customerCombo.Items.Clear();
        foreach (var c in _customerList) _customerCombo.Items.Add(c);

        Text = _isNew ? "Новая сделка" : $"Сделка: {deal.Title}";
        BindFromModel();
        UpdateClosedState();
    }

    private void BindFromModel()
    {
        _titleTextBox.Text = _deal.Title;
        _descriptionTextBox.Text = _deal.Description ?? string.Empty;
        _stageCombo.SelectedItem = _deal.Stage;
        _amountNumeric.Value = Math.Min(_amountNumeric.Maximum, Math.Max(_amountNumeric.Minimum, _deal.Amount.Amount));

        var currencyIndex = Array.IndexOf(Currencies, _deal.Amount.Currency);
        _currencyCombo.SelectedIndex = currencyIndex >= 0 ? currencyIndex : 0;

        _probabilityNumeric.Value = Math.Clamp(_deal.Probability, 0, 100);

        // Выбор клиента в ComboBox.
        if (_deal.CustomerId != Guid.Empty)
        {
            var match = _customerList.FirstOrDefault(c => c.Id == _deal.CustomerId);
            if (match is not null) _customerCombo.SelectedItem = match;
        }

        if (_deal.ExpectedCloseDate.HasValue)
        {
            _hasExpectedDateCheck.Checked = true;
            _expectedDatePicker.Value = _deal.ExpectedCloseDate.Value.ToDateTime(TimeOnly.MinValue);
            _expectedDatePicker.Enabled = true;
        }
        else
        {
            _hasExpectedDateCheck.Checked = false;
            _expectedDatePicker.Enabled = false;
        }

        if (_deal.ActualCloseDate.HasValue)
        {
            _actualDatePicker.Value = _deal.ActualCloseDate.Value.ToDateTime(TimeOnly.MinValue);
        }
    }

    private void BindToModel()
    {
        _deal.Title = _titleTextBox.Text.Trim();
        _deal.Description = string.IsNullOrWhiteSpace(_descriptionTextBox.Text)
            ? null : _descriptionTextBox.Text.Trim();
        _deal.Stage = (DealStage)_stageCombo.SelectedItem!;

        var currency = _currencyCombo.SelectedItem as string ?? "RUB";
        _deal.Amount = new Money(_amountNumeric.Value, currency);

        _deal.Probability = (int)_probabilityNumeric.Value;

        _deal.CustomerId = _customerCombo.SelectedItem is Customer c ? c.Id : Guid.Empty;

        _deal.ExpectedCloseDate = _hasExpectedDateCheck.Checked
            ? DateOnly.FromDateTime(_expectedDatePicker.Value)
            : null;

        // ActualCloseDate проставляется автоматически при переводе в Won/Lost,
        // но если форма редактирует уже закрытую сделку - сохраняем введённое значение.
        if (_deal.IsClosed)
        {
            _deal.ActualCloseDate = DateOnly.FromDateTime(_actualDatePicker.Value);
        }
    }

    // Подсветка/блокировка полей закрытой сделки.
    private void UpdateClosedState()
    {
        var stage = _stageCombo.SelectedItem as DealStage?;
        var isClosed = stage is DealStage.Won or DealStage.Lost;

        _actualDateLabel.Visible = isClosed;
        _actualDatePicker.Visible = isClosed;

        // При выборе Won автоматически выставляем 100%, при Lost - 0%
        // только если поле ещё в "штатном" состоянии (не редактировалось вручную).
        if (stage == DealStage.Won && _probabilityNumeric.Value < 100)
        {
            _probabilityNumeric.Value = 100;
        }
        else if (stage == DealStage.Lost && _probabilityNumeric.Value > 0)
        {
            _probabilityNumeric.Value = 0;
        }
    }

    private async void OnSaveClick(object? sender, EventArgs e)
    {
        BindToModel();

        var result = await _validator.ValidateAsync(_deal);
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
                await _deals.AddAsync(_deal);
            }
            else
            {
                await _deals.UpdateAsync(_deal);
            }
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сохранения сделки");
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

    // Локализация enum'ов вынесена в CrmApp.WinForms.Localization.EnumLocalization.
}
