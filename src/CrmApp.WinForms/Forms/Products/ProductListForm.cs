using System.ComponentModel;
using System.Diagnostics;
using CrmApp.Core.Abstractions;
using CrmApp.Core.Models;
using CrmApp.WinForms.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CrmApp.WinForms.Forms.Products;

public partial class ProductListForm : Form
{
    private readonly IProductRepository _repository;
    private readonly IServiceProvider _services;
    private readonly IExcelExportService _excel;
    private readonly ILogger<ProductListForm> _logger;
    private readonly BindingList<Product> _products = new();
    private ThreeStateSortManager<Product>? _sortManager;

    public ProductListForm(
        IProductRepository repository,
        IServiceProvider services,
        IExcelExportService excel,
        ILogger<ProductListForm> logger)
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
        Activated += async (_, _) => await ReloadAsync();
    }

    private void SetupGrid()
    {
        var source = new BindingSource { DataSource = _products };
        _grid.DataSource = source;
        _grid.AutoGenerateColumns = false;
        _grid.Columns.Clear();

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Product.Name),
            HeaderText = "Название",
            FillWeight = 32, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Product.Sku),
            HeaderText = "Артикул",
            FillWeight = 14, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Product.Category),
            HeaderText = "Категория",
            FillWeight = 18, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            // Money.ToString даёт уже отформатированную строку — поэтому не привязываемся к Price.Amount.
            DataPropertyName = nameof(Product.Price),
            HeaderText = "Цена",
            FillWeight = 16, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight },
        });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(Product.IsActive),
            HeaderText = "Активен",
            FillWeight = 10, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Product.UpdatedAt),
            HeaderText = "Обновлено",
            DefaultCellStyle = new DataGridViewCellStyle { Format = "g" },
            FillWeight = 14, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });

        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) EditSelected();
        };

        // 3-state-сортировка: цена сортируется как число (decimal), а не как форматированный текст.
        _sortManager = new ThreeStateSortManager<Product>(_grid, _products,
            new Dictionary<string, Func<Product, object?>>
            {
                [nameof(Product.Name)] = p => p.Name,
                [nameof(Product.Sku)] = p => p.Sku,
                [nameof(Product.Category)] = p => p.Category,
                [nameof(Product.Price)] = p => p.Price.Amount,
                [nameof(Product.IsActive)] = p => p.IsActive,
                [nameof(Product.UpdatedAt)] = p => p.UpdatedAt,
            });
    }

    private async Task ReloadAsync()
    {
        try
        {
            var items = await _repository.SearchAsync(_searchTextBox.Text);
            if (_activeOnlyCheck.Checked)
            {
                items = items.Where(p => p.IsActive).ToList();
            }

            var ordered = items.OrderBy(p => p.Name).ToList();
            if (_sortManager is not null)
            {
                _sortManager.Reset(ordered);
            }
            else
            {
                _products.Clear();
                foreach (var p in ordered) _products.Add(p);
            }

            _statusLabel.Text = $"Товаров: {_products.Count}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка загрузки списка товаров");
            MessageBox.Show("Не удалось загрузить товары: " + ex.Message,
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnAddClick(object? sender, EventArgs e)
    {
        var form = _services.GetRequiredService<ProductEditForm>();
        form.SetProduct(new Product());
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

        var form = _services.GetRequiredService<ProductEditForm>();
        form.SetProduct(Clone(selected));
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
            $"Удалить товар \"{selected.Name}\"?\nДействие необратимо.",
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
            _logger.LogError(ex, "Ошибка удаления товара {Id}", selected.Id);
            MessageBox.Show("Не удалось удалить: " + ex.Message,
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnExportClick(object? sender, EventArgs e)
    {
        if (_products.Count == 0)
        {
            MessageBox.Show("Список товаров пуст — нечего выгружать.",
                "Экспорт в Excel", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Filter = "Excel-файл (*.xlsx)|*.xlsx",
            FileName = $"Товары_{DateTime.Today:yyyy-MM-dd}.xlsx",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var headers = new[] { "Название", "Артикул", "Категория", "Цена", "Активен", "Описание", "Обновлено" };
            var rows = _products.Select(p => (IReadOnlyList<object?>)new object?[]
            {
                p.Name,
                p.Sku,
                p.Category,
                p.Price,
                p.IsActive,
                p.Description,
                p.UpdatedAt,
            }).ToList();

            await _excel.ExportAsync("Товары", headers, rows, dlg.FileName);
            OfferToOpen(dlg.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка экспорта товаров в Excel");
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

    private Product? GetSelected()
    {
        if (_grid.CurrentRow?.DataBoundItem is Product p) return p;
        MessageBox.Show("Выберите товар в таблице", "Внимание",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
        return null;
    }

    private static Product Clone(Product src) => new()
    {
        Id = src.Id,
        Name = src.Name,
        Sku = src.Sku,
        Description = src.Description,
        Price = src.Price,
        Category = src.Category,
        IsActive = src.IsActive,
        CreatedAt = src.CreatedAt,
        UpdatedAt = src.UpdatedAt,
    };
}
