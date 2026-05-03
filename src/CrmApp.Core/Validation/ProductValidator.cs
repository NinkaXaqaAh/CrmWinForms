using CrmApp.Core.Models;
using FluentValidation;

namespace CrmApp.Core.Validation;

public sealed class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("Название товара обязательно")
            .MaximumLength(200);

        RuleFor(p => p.Sku)
            .MaximumLength(50)
            .When(p => !string.IsNullOrWhiteSpace(p.Sku));

        RuleFor(p => p.Category)
            .MaximumLength(100)
            .When(p => !string.IsNullOrWhiteSpace(p.Category));

        RuleFor(p => p.Price.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Цена не может быть отрицательной");
    }
}
