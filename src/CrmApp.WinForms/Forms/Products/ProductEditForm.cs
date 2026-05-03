using CrmApp.Core.Abstractions;
using CrmApp.Core.Common;
using CrmApp.Core.Models;
using CrmApp.Core.Validation;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace CrmApp.WinForms.Forms.Products;

public partial class ProductEditForm : Form
{
    private readonly IProductRepository _repository;
    private readonly ProductValidator _validator;
    private readonly ILogger<ProductEditForm> _logger;

    private Product _product = new();
    private bool _isNew = true;

    private static readonly string[] Currencies = ["RUB", "USD", "EUR"];

    public ProductEditForm(
        IProductRepository repository,
        ProductValidator validator,
        ILogger<ProductEditForm> logger)
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
    }

    private void FillCombos()
    {
        _currencyCombo.Items.Clear();
        foreach (var c in Currencies) _currencyCombo.Items.Add(c);
    }

    public void SetProduct(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        _product = product;
        _isNew = product.CreatedAt == default;
        Text = _isNew ? "Новый товар" : $"Товар: {product.Name}";
        BindFromModel();
    }

    private void BindFromModel()
    {
        _nameTextBox.Text = _product.Name;
        _skuTextBox.Text = _product.Sku ?? string.Empty;
        _categoryTextBox.Text = _product.Category ?? string.Empty;
        _descriptionTextBox.Text = _product.Description ?? string.Empty;

        // Подгоняем сумму под допустимый диапазон ввода — на всякий случай.
        _priceNumeric.Value = Math.Min(_priceNumeric.Maximum,
            Math.Max(_priceNumeric.Minimum, _product.Price.Amount));

        var currencyIndex = Array.IndexOf(Currencies, _product.Price.Currency);
        _currencyCombo.SelectedIndex = currencyIndex >= 0 ? currencyIndex : 0;

        _isActiveCheck.Checked = _product.IsActive;
    }

    private void BindToModel()
    {
        _product.Name = _nameTextBox.Text.Trim();
        _product.Sku = NullIfBlank(_skuTextBox.Text);
        _product.Category = NullIfBlank(_categoryTextBox.Text);
        _product.Description = NullIfBlank(_descriptionTextBox.Text);

        var currency = _currencyCombo.SelectedItem as string ?? Money.DefaultCurrency;
        _product.Price = new Money(_priceNumeric.Value, currency);

        _product.IsActive = _isActiveCheck.Checked;
    }

    private static string? NullIfBlank(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private async void OnSaveClick(object? sender, EventArgs e)
    {
        BindToModel();

        var result = await _validator.ValidateAsync(_product);
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
                await _repository.AddAsync(_product);
            }
            else
            {
                await _repository.UpdateAsync(_product);
            }
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сохранения товара");
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
