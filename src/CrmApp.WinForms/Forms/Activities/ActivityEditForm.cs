using CrmApp.Core.Abstractions;
using CrmApp.Core.Enums;
using CrmApp.Core.Models;
using CrmApp.Core.Validation;
using CrmApp.WinForms.Localization;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace CrmApp.WinForms.Forms.Activities;

public partial class ActivityEditForm : Form
{
    private readonly IActivityRepository _activities;
    private readonly ICustomerRepository _customers;
    private readonly IDealRepository _deals;
    private readonly IDateTimeProvider _clock;
    private readonly ActivityValidator _validator;
    private readonly ILogger<ActivityEditForm> _logger;

    private Activity _activity = new();
    private bool _isNew = true;
    private List<Customer> _customerList = new();
    private List<Deal> _dealList = new();

    public ActivityEditForm(
        IActivityRepository activities,
        ICustomerRepository customers,
        IDealRepository deals,
        IDateTimeProvider clock,
        ActivityValidator validator,
        ILogger<ActivityEditForm> logger)
    {
        ArgumentNullException.ThrowIfNull(activities);
        ArgumentNullException.ThrowIfNull(customers);
        ArgumentNullException.ThrowIfNull(deals);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(logger);

        _activities = activities;
        _customers = customers;
        _deals = deals;
        _clock = clock;
        _validator = validator;
        _logger = logger;

        InitializeComponent();
        FillStaticCombos();

        AcceptButton = _saveButton;
        CancelButton = _cancelButton;

        _statusCombo.SelectedIndexChanged += (_, _) => UpdateCompletedVisibility();
        _customerCombo.SelectedIndexChanged += (_, _) => RefreshDealCombo();
    }

    // Все Format-обработчики подписываем РОВНО ОДИН РАЗ в конструкторе.
    // Если делать это в SetXxxAsync — handlers накапливаются при каждом открытии формы
    // (Transient в DI спасает только потому что экземпляр одноразовый, но сам паттерн фрагилен).
    // А для _dealCombo подписки не было вовсе — combo печатал obj.ToString() = "CrmApp.Core.Models.Deal".
    private void FillStaticCombos()
    {
        _typeCombo.Items.Clear();
        foreach (var t in Enum.GetValues<ActivityType>()) _typeCombo.Items.Add(t);
        _typeCombo.Format += (_, e) =>
        {
            if (e.ListItem is ActivityType t) e.Value = t.ToRussian();
        };

        _statusCombo.Items.Clear();
        foreach (var s in Enum.GetValues<ActivityStatus>()) _statusCombo.Items.Add(s);
        _statusCombo.Format += (_, e) =>
        {
            if (e.ListItem is ActivityStatus s) e.Value = s.ToRussian();
        };

        _priorityCombo.Items.Clear();
        foreach (var p in Enum.GetValues<Priority>()) _priorityCombo.Items.Add(p);
        _priorityCombo.Format += (_, e) =>
        {
            if (e.ListItem is Priority p) e.Value = p.ToRussian();
        };

        // Format для customer и deal — подписываются ДО заполнения Items, чтобы при первом
        // же добавлении уже работали. Items придут позже из SetActivityAsync.
        _customerCombo.Format += (_, e) =>
        {
            if (e.ListItem is Customer c) e.Value = c.DisplayName;
        };
        _dealCombo.Format += (_, e) =>
        {
            if (e.ListItem is Deal d) e.Value = d.Title;
        };
    }

    public async Task SetActivityAsync(Activity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        _activity = activity;
        _isNew = activity.CreatedAt == default;

        _customerList = (await _customers.GetAllAsync())
            .OrderBy(c => c.DisplayName)
            .ToList();
        _dealList = (await _deals.GetAllAsync())
            .OrderBy(d => d.Title)
            .ToList();

        // (нет) — пустой пункт-пустышка для очистки выбора.
        _customerCombo.Items.Clear();
        _customerCombo.Items.Add("(нет)");
        foreach (var c in _customerList) _customerCombo.Items.Add(c);

        Text = _isNew ? "Новая активность" : $"Активность: {activity.Title}";
        BindFromModel();
        UpdateCompletedVisibility();
    }

    private void BindFromModel()
    {
        _typeCombo.SelectedItem = _activity.Type;
        _statusCombo.SelectedItem = _activity.Status;
        _priorityCombo.SelectedItem = _activity.Priority;
        _titleTextBox.Text = _activity.Title;
        _descriptionTextBox.Text = _activity.Description ?? string.Empty;

        _dueDatePicker.Value = ClampToPickerRange(_activity.DueDate);

        if (_activity.CompletedAt.HasValue)
        {
            _completedAtPicker.Value = ClampToPickerRange(_activity.CompletedAt.Value);
        }
        else
        {
            _completedAtPicker.Value = ClampToPickerRange(_clock.Now);
        }

        if (_activity.CustomerId.HasValue)
        {
            var match = _customerList.FirstOrDefault(c => c.Id == _activity.CustomerId.Value);
            _customerCombo.SelectedItem = (object?)match ?? _customerCombo.Items[0]!;
        }
        else
        {
            _customerCombo.SelectedIndex = 0;
        }

        RefreshDealCombo();

        if (_activity.DealId.HasValue)
        {
            var match = _dealList.FirstOrDefault(d => d.Id == _activity.DealId.Value);
            _dealCombo.SelectedItem = (object?)match ?? _dealCombo.Items[0]!;
        }
        else if (_dealCombo.Items.Count > 0)
        {
            _dealCombo.SelectedIndex = 0;
        }
    }

    // Когда меняется выбранный клиент — список сделок фильтруем под него,
    // плюс пункт "(нет)" для возможности отвязать.
    private void RefreshDealCombo()
    {
        _dealCombo.Items.Clear();
        _dealCombo.Items.Add("(нет)");

        var selectedCustomer = _customerCombo.SelectedItem as Customer;
        var deals = selectedCustomer is null
            ? _dealList
            : _dealList.Where(d => d.CustomerId == selectedCustomer.Id).ToList();

        foreach (var d in deals) _dealCombo.Items.Add(d);

        if (_dealCombo.Items.Count > 0) _dealCombo.SelectedIndex = 0;
    }

    private void BindToModel()
    {
        _activity.Type = (ActivityType)_typeCombo.SelectedItem!;
        _activity.Status = (ActivityStatus)_statusCombo.SelectedItem!;
        _activity.Priority = (Priority)_priorityCombo.SelectedItem!;
        _activity.Title = _titleTextBox.Text.Trim();
        _activity.Description = string.IsNullOrWhiteSpace(_descriptionTextBox.Text)
            ? null : _descriptionTextBox.Text.Trim();
        _activity.DueDate = _dueDatePicker.Value;

        _activity.CustomerId = _customerCombo.SelectedItem is Customer c ? c.Id : null;
        _activity.DealId = _dealCombo.SelectedItem is Deal d ? d.Id : null;

        // CompletedAt сохраняем только для завершённых.
        _activity.CompletedAt = _activity.Status == ActivityStatus.Completed
            ? _completedAtPicker.Value
            : null;
    }

    private void UpdateCompletedVisibility()
    {
        var isCompleted = (_statusCombo.SelectedItem as ActivityStatus?) == ActivityStatus.Completed;
        _completedAtLabel.Visible = isCompleted;
        _completedAtPicker.Visible = isCompleted;
    }

    private async void OnSaveClick(object? sender, EventArgs e)
    {
        BindToModel();

        var result = await _validator.ValidateAsync(_activity);
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
                await _activities.AddAsync(_activity);
            }
            else
            {
                await _activities.UpdateAsync(_activity);
            }
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сохранения активности");
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

    // DateTimePicker не любит значения вне MinDate/MaxDate (по умолчанию 1753..9998).
    // Если в репозитории случайно хранится default(DateTime), DateTime.MinValue, и т.п. —
    // подгоняем к ближайшему допустимому значению вместо падения.
    private static DateTime ClampToPickerRange(DateTime value)
    {
        if (value < DateTimePicker.MinimumDateTime) return DateTimePicker.MinimumDateTime;
        if (value > DateTimePicker.MaximumDateTime) return DateTimePicker.MaximumDateTime;
        return value;
    }

    // Локализация enum'ов вынесена в CrmApp.WinForms.Localization.EnumLocalization (extension ToRussian).
}
