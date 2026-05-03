using System.ComponentModel;
using System.Diagnostics;
using CrmApp.Core.Abstractions;
using CrmApp.Core.Enums;
using CrmApp.Core.Models;
using CrmApp.WinForms.Controls;
using CrmApp.WinForms.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CrmApp.WinForms.Forms.Customers;

public partial class CustomerListForm : Form
{
    private readonly ICustomerRepository _repository;
    private readonly IServiceProvider _services;
    private readonly IExcelExportService _excel;
    private readonly ILogger<CustomerListForm> _logger;
    private readonly BindingList<Customer> _customers = new();
    private ThreeStateSortManager<Customer>? _sortManager;

    public CustomerListForm(
        ICustomerRepository repository,
        IServiceProvider services,
        IExcelExportService excel,
        ILogger<CustomerListForm> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(excel);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _services = services;
        _excel = excel;
        _logger = logger;

        InitializeComponent();
        SetupGrid();

        Load += async (_, _) => await ReloadAsync();
        // Activated срабатывает при возврате с модального диалога и при переключении
        // между MDI-окнами — благодаря этому список сам обновляется после правок
        // в других местах (например, добавили клиента в форме сделки).
        Activated += async (_, _) => await ReloadAsync();
    }

    private void SetupGrid()
    {
        // Привязка через BindingSource - стандартный паттерн для DataGridView.
        var source = new BindingSource { DataSource = _customers };
        _grid.DataSource = source;

        _grid.AutoGenerateColumns = false;
        _grid.Columns.Clear();
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Customer.DisplayName),
            HeaderText = "Имя / Компания",
            FillWeight = 30,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Customer.Type),
            HeaderText = "Тип",
            FillWeight = 12,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Customer.Status),
            HeaderText = "Статус",
            FillWeight = 12,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Customer.Phone),
            HeaderText = "Телефон",
            FillWeight = 18,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Customer.Email),
            HeaderText = "Email",
            FillWeight = 20,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Customer.UpdatedAt),
            HeaderText = "Обновлено",
            DefaultCellStyle = new DataGridViewCellStyle { Format = "g" },
            FillWeight = 14,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });

        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) EditSelected();
        };

        // Локализация значений в гриде (Тип, Статус хранятся как enum, но показываются по-русски).
        _grid.CellFormatting += (_, e) =>
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0) return;
            var col = _grid.Columns[e.ColumnIndex];
            if (col.DataPropertyName == nameof(Customer.Type) && e.Value is CustomerType ct)
            {
                e.Value = ct.ToRussian();
                e.FormattingApplied = true;
            }
            else if (col.DataPropertyName == nameof(Customer.Status) && e.Value is CustomerStatus cs)
            {
                e.Value = cs.ToRussian();
                e.FormattingApplied = true;
            }
        };

        // Заполняем фильтр статусов. Format-обработчик локализует enum-итемы при показе.
        _statusFilter.Items.Clear();
        _statusFilter.Items.Add("Все статусы");
        foreach (var s in Enum.GetValues<CustomerStatus>())
        {
            _statusFilter.Items.Add(s);
        }
        _statusFilter.Format += (_, e) =>
        {
            if (e.ListItem is CustomerStatus s) e.Value = s.ToRussian();
        };
        _statusFilter.SelectedIndex = 0;

        // 3-state-сортировка по клику на заголовок: enum-колонки сортируем по русскому
        // отображению, чтобы пользователь видел результат "по алфавиту", как и в гриде.
        _sortManager = new ThreeStateSortManager<Customer>(_grid, _customers,
            new Dictionary<string, Func<Customer, object?>>
            {
                [nameof(Customer.DisplayName)] = c => c.DisplayName,
                [nameof(Customer.Type)] = c => c.Type.ToRussian(),
                [nameof(Customer.Status)] = c => c.Status.ToRussian(),
                [nameof(Customer.Phone)] = c => c.Phone,
                [nameof(Customer.Email)] = c => c.Email,
                [nameof(Customer.UpdatedAt)] = c => c.UpdatedAt,
            });
    }

    private async Task ReloadAsync()
    {
        try
        {
            var search = _searchTextBox.Text.Trim();
            var items = await _repository.SearchAsync(search);

            // Дополнительный фильтр по статусу - на стороне клиента,
            // чтобы не плодить методов в репозитории под каждое сочетание.
            if (_statusFilter.SelectedItem is CustomerStatus status)
            {
                items = items.Where(c => c.Status == status).ToList();
            }

            // Передаём данные через sortManager — он держит "оригинальный порядок"
            // (по которому возвращаемся при 3-м клике на заголовок) и применяет текущую сортировку.
            var ordered = items.OrderByDescending(c => c.UpdatedAt).ToList();
            if (_sortManager is not null)
            {
                _sortManager.Reset(ordered);
            }
            else
            {
                _customers.Clear();
                foreach (var c in ordered) _customers.Add(c);
            }

            _statusLabel.Text = $"Всего: {_customers.Count}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка загрузки клиентов");
            MessageBox.Show("Не удалось загрузить список клиентов: " + ex.Message,
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnAddClick(object? sender, EventArgs e)
    {
        var form = _services.GetRequiredService<CustomerEditForm>();
        form.SetCustomer(new Customer());
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await ReloadAsync();
        }
    }

    private void OnEditClick(object? sender, EventArgs e) => EditSelected();

    private async void EditSelected()
    {
        var selected = GetSelected();
        if (selected is null) return;

        var form = _services.GetRequiredService<CustomerEditForm>();
        // Передаём копию, чтобы при отмене изменения не остались в кэше.
        form.SetCustomer(Clone(selected));
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await ReloadAsync();
        }
    }

    private async void OnDeleteClick(object? sender, EventArgs e)
    {
        var selected = GetSelected();
        if (selected is null) return;

        var ok = MessageBox.Show(
            $"Удалить клиента \"{selected.DisplayName}\"?\nДействие необратимо.",
            "Удаление",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (ok != DialogResult.Yes) return;

        try
        {
            await _repository.DeleteAsync(selected.Id);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка удаления клиента {Id}", selected.Id);
            MessageBox.Show("Не удалось удалить: " + ex.Message,
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnExportClick(object? sender, EventArgs e)
    {
        if (_customers.Count == 0)
        {
            MessageBox.Show("Список клиентов пуст — нечего выгружать.",
                "Экспорт в Excel", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Filter = "Excel-файл (*.xlsx)|*.xlsx",
            FileName = $"Клиенты_{DateTime.Today:yyyy-MM-dd}.xlsx",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var headers = new[]
            {
                "Имя / Компания", "Тип", "Статус",
                "Телефон", "Email", "Адрес", "Заметки", "Обновлено",
            };
            var rows = _customers.Select(c => (IReadOnlyList<object?>)new object?[]
            {
                c.DisplayName,
                c.Type.ToRussian(),
                c.Status.ToRussian(),
                c.Phone,
                c.Email,
                c.Address,
                c.Notes,
                c.UpdatedAt,
            }).ToList();

            await _excel.ExportAsync("Клиенты", headers, rows, dlg.FileName);
            OfferToOpen(dlg.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка экспорта клиентов в Excel");
            MessageBox.Show("Не удалось сохранить файл: " + ex.Message,
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Спрашиваем, надо ли открыть только что сохранённый файл.
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

    private Customer? GetSelected()
    {
        if (_grid.CurrentRow?.DataBoundItem is Customer c) return c;
        MessageBox.Show("Выберите клиента в таблице", "Внимание",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
        return null;
    }

    // Простой клон через копирование полей. AutoMapper здесь избыточен.
    private static Customer Clone(Customer src) => new()
    {
        Id = src.Id,
        Type = src.Type,
        Status = src.Status,
        Name = src.Name,
        Phone = src.Phone,
        Email = src.Email,
        Address = src.Address,
        Notes = src.Notes,
        CompanyName = src.CompanyName,
        Inn = src.Inn,
        Position = src.Position,
        BirthDate = src.BirthDate,
        AssignedUserId = src.AssignedUserId,
        CreatedAt = src.CreatedAt,
        UpdatedAt = src.UpdatedAt,
    };
}
