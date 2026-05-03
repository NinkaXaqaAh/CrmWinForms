using System.ComponentModel;
// using System.Diagnostics напрямую брать нельзя: System.Diagnostics.Activity конфликтует
// с CrmApp.Core.Models.Activity. Подтягиваем только конкретные типы через alias.
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using CrmApp.Core.Abstractions;
using CrmApp.Core.Enums;
using CrmApp.Core.Models;
using CrmApp.WinForms.Controls;
using CrmApp.WinForms.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CrmApp.WinForms.Forms.Activities;

public partial class ActivityListForm : Form
{
    private readonly IActivityRepository _activities;
    private readonly ICustomerRepository _customers;
    private readonly IDealRepository _deals;
    private readonly IDateTimeProvider _clock;
    private readonly IServiceProvider _services;
    private readonly IExcelExportService _excel;
    private readonly ILogger<ActivityListForm> _logger;

    private readonly BindingList<ActivityRow> _rows = new();
    private ThreeStateSortManager<ActivityRow>? _sortManager;

    public ActivityListForm(
        IActivityRepository activities,
        ICustomerRepository customers,
        IDealRepository deals,
        IDateTimeProvider clock,
        IServiceProvider services,
        IExcelExportService excel,
        ILogger<ActivityListForm> logger)
    {
        ArgumentNullException.ThrowIfNull(activities);
        ArgumentNullException.ThrowIfNull(customers);
        ArgumentNullException.ThrowIfNull(deals);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(excel);
        ArgumentNullException.ThrowIfNull(logger);

        _activities = activities;
        _customers = customers;
        _deals = deals;
        _clock = clock;
        _services = services;
        _excel = excel;
        _logger = logger;

        InitializeComponent();
        SetupGrid();

        Load += async (_, _) => await ReloadAsync();
        Activated += async (_, _) => await ReloadAsync();
    }

    private void SetupGrid()
    {
        _grid.DataSource = new BindingSource { DataSource = _rows };
        _grid.AutoGenerateColumns = false;
        _grid.Columns.Clear();

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(ActivityRow.DueDateDisplay),
            HeaderText = "Срок",
            FillWeight = 14, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(ActivityRow.Title),
            HeaderText = "Заголовок",
            FillWeight = 26, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(ActivityRow.TypeDisplay),
            HeaderText = "Тип",
            FillWeight = 10, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(ActivityRow.PriorityDisplay),
            HeaderText = "Приоритет",
            FillWeight = 10, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(ActivityRow.StatusDisplay),
            HeaderText = "Статус",
            FillWeight = 12, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(ActivityRow.CustomerName),
            HeaderText = "Клиент",
            FillWeight = 16, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(ActivityRow.DealTitle),
            HeaderText = "Сделка",
            FillWeight = 12, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });

        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditSelected(); };
        _grid.RowPrePaint += OnRowPrePaint;

        _statusFilter.Items.Clear();
        _statusFilter.Items.Add("Все статусы");
        foreach (var s in Enum.GetValues<ActivityStatus>()) _statusFilter.Items.Add(s);
        _statusFilter.Format += (_, e) =>
        {
            if (e.ListItem is ActivityStatus s) e.Value = s.ToRussian();
        };
        _statusFilter.SelectedIndex = 0;

        // Сортировка: даты сравниваем как DateTime, текстовые колонки — алфавитно по русскому отображению.
        _sortManager = new ThreeStateSortManager<ActivityRow>(_grid, _rows,
            new Dictionary<string, Func<ActivityRow, object?>>
            {
                [nameof(ActivityRow.DueDateDisplay)] = r => r.Source.DueDate,
                [nameof(ActivityRow.Title)] = r => r.Title,
                [nameof(ActivityRow.TypeDisplay)] = r => r.TypeDisplay,
                [nameof(ActivityRow.PriorityDisplay)] = r => r.PriorityDisplay,
                [nameof(ActivityRow.StatusDisplay)] = r => r.StatusDisplay,
                [nameof(ActivityRow.CustomerName)] = r => r.CustomerName,
                [nameof(ActivityRow.DealTitle)] = r => r.DealTitle,
            });
    }

    // Просрочка — красноватый фон. Завершённые — приглушённый серый.
    // Цвета берутся из AppPalette, чтобы корректно работать в обеих темах (светлой и тёмной).
    private void OnRowPrePaint(object? sender, DataGridViewRowPrePaintEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _rows.Count) return;
        var row = _rows[e.RowIndex];
        var style = _grid.Rows[e.RowIndex].DefaultCellStyle;

        if (row.Source.Status == ActivityStatus.Completed)
        {
            style.BackColor = Theming.AppPalette.RowCompletedBackground;
            style.ForeColor = Theming.AppPalette.RowCompletedForeground;
        }
        else if (row.IsOverdue)
        {
            style.BackColor = Theming.AppPalette.RowOverdueBackground;
            style.ForeColor = Theming.AppPalette.RowOverdueForeground;
        }
        else
        {
            style.BackColor = Theming.AppPalette.Surface;
            style.ForeColor = Theming.AppPalette.TextPrimary;
        }
    }

    private async Task ReloadAsync()
    {
        try
        {
            var items = (await _activities.GetAllAsync()).ToList();
            var customersById = (await _customers.GetAllAsync()).ToDictionary(c => c.Id);
            var dealsById = (await _deals.GetAllAsync()).ToDictionary(d => d.Id);

            if (_statusFilter.SelectedItem is ActivityStatus status)
            {
                items = items.Where(a => a.Status == status).ToList();
            }

            if (_overdueOnlyCheck.Checked)
            {
                var now = _clock.Now;
                items = items.Where(a => a.IsOverdue(now)).ToList();
            }

            var search = _searchTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                items = items.Where(a =>
                    a.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (a.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
            }

            var nowForRows = _clock.Now;
            var rows = items.OrderBy(a => a.DueDate)
                .Select(a =>
                {
                    Customer? customer = a.CustomerId.HasValue && customersById.TryGetValue(a.CustomerId.Value, out var c) ? c : null;
                    Deal? deal = a.DealId.HasValue && dealsById.TryGetValue(a.DealId.Value, out var d) ? d : null;
                    return new ActivityRow(a, customer?.DisplayName ?? "—", deal?.Title ?? "—", a.IsOverdue(nowForRows));
                })
                .ToList();

            if (_sortManager is not null)
            {
                _sortManager.Reset(rows);
            }
            else
            {
                _rows.Clear();
                foreach (var r in rows) _rows.Add(r);
            }

            _statusLabel.Text = $"Активностей: {_rows.Count}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка загрузки активностей");
            MessageBox.Show("Не удалось загрузить активности: " + ex.Message,
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnAddClick(object? sender, EventArgs e)
    {
        var form = _services.GetRequiredService<ActivityEditForm>();
        await form.SetActivityAsync(new Activity { DueDate = _clock.Now.AddDays(1) });
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await ReloadAsync();
        }
    }

    private void OnEditClick(object? sender, EventArgs e) => EditSelected();

    private async void EditSelected()
    {
        var row = GetSelected();
        if (row is null) return;

        var form = _services.GetRequiredService<ActivityEditForm>();
        await form.SetActivityAsync(Clone(row.Source));
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await ReloadAsync();
        }
    }

    // Быстрое действие "выполнено".
    private async void OnCompleteClick(object? sender, EventArgs e)
    {
        var row = GetSelected();
        if (row is null) return;

        if (row.Source.Status == ActivityStatus.Completed)
        {
            MessageBox.Show("Активность уже завершена", "Информация",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var copy = Clone(row.Source);
            copy.Status = ActivityStatus.Completed;
            copy.CompletedAt = _clock.Now;
            await _activities.UpdateAsync(copy);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка пометки активности как завершённой");
            MessageBox.Show("Не удалось обновить: " + ex.Message,
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnDeleteClick(object? sender, EventArgs e)
    {
        var row = GetSelected();
        if (row is null) return;

        var ok = MessageBox.Show($"Удалить активность \"{row.Title}\"?", "Удаление",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (ok != DialogResult.Yes) return;

        try
        {
            await _activities.DeleteAsync(row.Source.Id);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка удаления активности");
            MessageBox.Show("Не удалось удалить: " + ex.Message,
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnExportClick(object? sender, EventArgs e)
    {
        if (_rows.Count == 0)
        {
            MessageBox.Show("Список активностей пуст — нечего выгружать.",
                "Экспорт в Excel", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Filter = "Excel-файл (*.xlsx)|*.xlsx",
            FileName = $"Активности_{DateTime.Today:yyyy-MM-dd}.xlsx",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var headers = new[]
            {
                "Срок", "Заголовок", "Тип", "Приоритет",
                "Статус", "Клиент", "Сделка", "Просрочена",
            };
            var rows = _rows.Select(r => (IReadOnlyList<object?>)new object?[]
            {
                r.Source.DueDate,
                r.Title,
                r.TypeDisplay,
                r.PriorityDisplay,
                r.StatusDisplay,
                r.CustomerName,
                r.DealTitle,
                r.IsOverdue,
            }).ToList();

            await _excel.ExportAsync("Активности", headers, rows, dlg.FileName);
            OfferToOpen(dlg.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка экспорта активностей в Excel");
            MessageBox.Show("Не удалось сохранить файл: " + ex.Message,
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OfferToOpen(string filePath)
    {
        var result = MessageBox.Show(
            $"Файл сохранён:\n{filePath}\n\nОткрыть его сейчас?",
            "Готово",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);
        if (result != DialogResult.Yes) return;
        try
        {
            Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось открыть файл {File}", filePath);
        }
    }

    private async void OnSearchClick(object? sender, EventArgs e) => await ReloadAsync();
    private async void OnFilterChanged(object? sender, EventArgs e) => await ReloadAsync();

    private async void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            await ReloadAsync();
        }
    }

    private ActivityRow? GetSelected()
    {
        if (_grid.CurrentRow?.DataBoundItem is ActivityRow row) return row;
        MessageBox.Show("Выберите активность в таблице", "Внимание",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
        return null;
    }

    private static Activity Clone(Activity a) => new()
    {
        Id = a.Id,
        Type = a.Type,
        Status = a.Status,
        Priority = a.Priority,
        Title = a.Title,
        Description = a.Description,
        CustomerId = a.CustomerId,
        DealId = a.DealId,
        AssignedUserId = a.AssignedUserId,
        DueDate = a.DueDate,
        CompletedAt = a.CompletedAt,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt,
    };

    public sealed class ActivityRow
    {
        public ActivityRow(Activity source, string customerName, string dealTitle, bool isOverdue)
        {
            Source = source;
            CustomerName = customerName;
            DealTitle = dealTitle;
            IsOverdue = isOverdue;
        }

        public Activity Source { get; }
        public string Title => Source.Title;
        public string CustomerName { get; }
        public string DealTitle { get; }
        public bool IsOverdue { get; }
        public string DueDateDisplay => Source.DueDate.ToString("dd.MM.yyyy HH:mm");
        public string TypeDisplay => Source.Type.ToRussian();
        public string StatusDisplay => Source.Status.ToRussian();
        public string PriorityDisplay => Source.Priority.ToRussian();
    }
}
