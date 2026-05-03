using System.ComponentModel;
using System.Diagnostics;
using CrmApp.Core.Abstractions;
using CrmApp.Core.Enums;
using CrmApp.Core.Models;
using CrmApp.WinForms.Controls;
using CrmApp.WinForms.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CrmApp.WinForms.Forms.Deals;

public partial class DealListForm : Form
{
    private readonly IDealRepository _deals;
    private readonly ICustomerRepository _customers;
    private readonly IActivityRepository _activities;
    private readonly IServiceProvider _services;
    private readonly IExcelExportService _excel;
    private readonly IPdfReportService _pdf;
    private readonly ILogger<DealListForm> _logger;

    private readonly BindingList<DealRow> _rows = new();
    private ThreeStateSortManager<DealRow>? _sortManager;

    public DealListForm(
        IDealRepository deals,
        ICustomerRepository customers,
        IActivityRepository activities,
        IServiceProvider services,
        IExcelExportService excel,
        IPdfReportService pdf,
        ILogger<DealListForm> logger)
    {
        ArgumentNullException.ThrowIfNull(deals);
        ArgumentNullException.ThrowIfNull(customers);
        ArgumentNullException.ThrowIfNull(activities);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(excel);
        ArgumentNullException.ThrowIfNull(pdf);
        ArgumentNullException.ThrowIfNull(logger);

        _deals = deals;
        _customers = customers;
        _activities = activities;
        _services = services;
        _excel = excel;
        _pdf = pdf;
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
            DataPropertyName = nameof(DealRow.Title),
            HeaderText = "Название",
            FillWeight = 30, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(DealRow.CustomerName),
            HeaderText = "Клиент",
            FillWeight = 22, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(DealRow.StageDisplay),
            HeaderText = "Этап",
            FillWeight = 14, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(DealRow.AmountDisplay),
            HeaderText = "Сумма",
            FillWeight = 12, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight },
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(DealRow.Probability),
            HeaderText = "Вероятность",
            FillWeight = 10, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleRight,
                Format = "0'%'",
            },
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(DealRow.ExpectedCloseDisplay),
            HeaderText = "Закрытие",
            FillWeight = 12, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });

        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) EditSelected();
        };

        // Раскраска строк по этапу - тонкий фон в боковом столбце через CellFormatting.
        _grid.RowPrePaint += OnRowPrePaint;

        // Фильтр по этапу. Format-обработчик локализует элементы при показе.
        _stageFilter.Items.Clear();
        _stageFilter.Items.Add("Все этапы");
        foreach (var s in Enum.GetValues<DealStage>())
        {
            _stageFilter.Items.Add(s);
        }
        _stageFilter.Format += (_, e) =>
        {
            if (e.ListItem is DealStage s) e.Value = s.ToRussian();
        };
        _stageFilter.SelectedIndex = 0;

        // 3-state-сортировка. Для денег и вероятности сортируем по числу (decimal/int),
        // не по форматированной строке — иначе "1 250 000,00 ₽" окажется меньше "95 000,00 ₽".
        _sortManager = new ThreeStateSortManager<DealRow>(_grid, _rows,
            new Dictionary<string, Func<DealRow, object?>>
            {
                [nameof(DealRow.Title)] = r => r.Title,
                [nameof(DealRow.CustomerName)] = r => r.CustomerName,
                [nameof(DealRow.StageDisplay)] = r => r.StageDisplay,
                [nameof(DealRow.AmountDisplay)] = r => r.Source.Amount.Amount,
                [nameof(DealRow.Probability)] = r => r.Probability,
                [nameof(DealRow.ExpectedCloseDisplay)] = r => r.Source.ExpectedCloseDate,
            });
    }

    // Подсветка строк выигранных/проигранных сделок. Цвета через AppPalette,
    // чтобы оба варианта темы (светлая/тёмная) выглядели корректно.
    private void OnRowPrePaint(object? sender, DataGridViewRowPrePaintEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _rows.Count) return;
        var row = _rows[e.RowIndex];
        var bg = row.Stage switch
        {
            DealStage.Won => Theming.AppPalette.RowWonBackground,
            DealStage.Lost => Theming.AppPalette.RowLostBackground,
            _ => Theming.AppPalette.Surface,
        };
        _grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = bg;
    }

    private async Task ReloadAsync()
    {
        try
        {
            var deals = await _deals.GetAllAsync();
            var customers = (await _customers.GetAllAsync()).ToDictionary(c => c.Id);

            if (_stageFilter.SelectedItem is DealStage stage)
            {
                deals = deals.Where(d => d.Stage == stage).ToList();
            }

            var search = _searchTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                deals = deals.Where(d =>
                    d.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (d.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
            }

            var rows = deals.OrderByDescending(x => x.UpdatedAt)
                .Select(d =>
                {
                    customers.TryGetValue(d.CustomerId, out var c);
                    return new DealRow(d, c?.DisplayName ?? "(нет клиента)");
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

            _statusLabel.Text = $"Сделок: {_rows.Count}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка загрузки сделок");
            MessageBox.Show("Не удалось загрузить сделки: " + ex.Message,
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnAddClick(object? sender, EventArgs e)
    {
        var form = _services.GetRequiredService<DealEditForm>();
        await form.SetDealAsync(new Deal());
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

        var form = _services.GetRequiredService<DealEditForm>();
        await form.SetDealAsync(CloneDeal(row.Source));
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await ReloadAsync();
        }
    }

    private async void OnDeleteClick(object? sender, EventArgs e)
    {
        var row = GetSelected();
        if (row is null) return;

        var ok = MessageBox.Show($"Удалить сделку \"{row.Title}\"?", "Удаление",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (ok != DialogResult.Yes) return;

        try
        {
            await _deals.DeleteAsync(row.Source.Id);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка удаления сделки");
            MessageBox.Show("Не удалось удалить: " + ex.Message,
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnExportClick(object? sender, EventArgs e)
    {
        if (_rows.Count == 0)
        {
            MessageBox.Show("Список сделок пуст — нечего выгружать.",
                "Экспорт в Excel", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Filter = "Excel-файл (*.xlsx)|*.xlsx",
            FileName = $"Сделки_{DateTime.Today:yyyy-MM-dd}.xlsx",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var headers = new[]
            {
                "Название", "Клиент", "Этап",
                "Сумма", "Вероятность, %", "Ожидаемое закрытие", "Фактическое закрытие",
            };
            var rows = _rows.Select(r => (IReadOnlyList<object?>)new object?[]
            {
                r.Title,
                r.CustomerName,
                r.StageDisplay,
                r.Source.Amount,
                r.Probability,
                r.Source.ExpectedCloseDate,
                r.Source.ActualCloseDate,
            }).ToList();

            await _excel.ExportAsync("Сделки", headers, rows, dlg.FileName);
            OfferToOpen(dlg.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка экспорта сделок в Excel");
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

    // PDF-отчёт по выбранной сделке.
    // Подгружаем клиента и связанные активности, чтобы PDF был самодостаточным.
    private async void OnPdfClick(object? sender, EventArgs e)
    {
        var row = GetSelected();
        if (row is null) return;

        using var dlg = new SaveFileDialog
        {
            Filter = "PDF-документ (*.pdf)|*.pdf",
            FileName = $"Сделка_{SanitizeFileName(row.Title)}_{DateTime.Today:yyyy-MM-dd}.pdf",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var customer = await _customers.FindByIdAsync(row.Source.CustomerId);
            var activities = await _activities.GetByDealAsync(row.Source.Id);

            await _pdf.GenerateDealReportAsync(row.Source, customer, activities, dlg.FileName);
            OfferToOpen(dlg.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка генерации PDF-отчёта по сделке {Id}", row.Source.Id);
            MessageBox.Show("Не удалось сохранить PDF: " + ex.Message,
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Чистим имя для безопасного использования в имени файла.
    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = string.Concat(name.Where(c => !invalid.Contains(c)));
        return clean.Length > 60 ? clean[..60] : clean;
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

    private DealRow? GetSelected()
    {
        if (_grid.CurrentRow?.DataBoundItem is DealRow row) return row;
        MessageBox.Show("Выберите сделку в таблице", "Внимание",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
        return null;
    }

    private static Deal CloneDeal(Deal d) => new()
    {
        Id = d.Id,
        Title = d.Title,
        Description = d.Description,
        Stage = d.Stage,
        Amount = d.Amount,
        Probability = d.Probability,
        CustomerId = d.CustomerId,
        AssignedUserId = d.AssignedUserId,
        ExpectedCloseDate = d.ExpectedCloseDate,
        ActualCloseDate = d.ActualCloseDate,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt,
    };

    // DTO для отображения - чтобы не показывать Guid'ы и форматировать сумму один раз.
    public sealed class DealRow
    {
        public DealRow(Deal source, string customerName)
        {
            Source = source;
            CustomerName = customerName;
        }

        public Deal Source { get; }
        public string Title => Source.Title;
        public string CustomerName { get; }
        public DealStage Stage => Source.Stage;
        public string StageDisplay => Source.Stage.ToRussian();
        public string AmountDisplay => Source.Amount.ToString();
        public int Probability => Source.Probability;
        public string ExpectedCloseDisplay => Source.ExpectedCloseDate?.ToString("dd.MM.yyyy") ?? "—";
    }
}
