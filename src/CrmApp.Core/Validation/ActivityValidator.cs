using CrmApp.Core.Models;
using FluentValidation;

namespace CrmApp.Core.Validation;

public sealed class ActivityValidator : AbstractValidator<Activity>
{
    public ActivityValidator()
    {
        RuleFor(a => a.Title)
            .NotEmpty().WithMessage("Заголовок активности обязателен")
            .MaximumLength(200);

        RuleFor(a => a.DueDate)
            .Must(d => d.Year >= 2000 && d.Year <= 2100)
            .WithMessage("Дата выполнения некорректна");
    }
}
