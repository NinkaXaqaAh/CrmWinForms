using CrmApp.Core.Models;
using FluentValidation;

namespace CrmApp.Core.Validation;

public sealed class DealValidator : AbstractValidator<Deal>
{
    public DealValidator()
    {
        RuleFor(d => d.Title)
            .NotEmpty().WithMessage("Название сделки обязательно")
            .MaximumLength(200);

        RuleFor(d => d.CustomerId)
            .NotEqual(Guid.Empty).WithMessage("Выберите клиента");

        RuleFor(d => d.Amount.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Сумма не может быть отрицательной");

        RuleFor(d => d.Probability)
            .InclusiveBetween(0, 100).WithMessage("Вероятность должна быть от 0 до 100%");

        // Если задана ожидаемая дата закрытия и сделка ещё открыта,
        // дата не должна быть в далёком прошлом (сигнал, что забыли обновить).
        RuleFor(d => d.ExpectedCloseDate)
            .Must(d => d == null || d >= DateOnly.FromDateTime(DateTime.Today.AddYears(-1)))
            .WithMessage("Ожидаемая дата закрытия слишком далеко в прошлом");
    }
}
