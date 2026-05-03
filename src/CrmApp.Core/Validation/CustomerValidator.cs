using CrmApp.Core.Enums;
using CrmApp.Core.Models;
using FluentValidation;

namespace CrmApp.Core.Validation;

// Валидатор клиента.
// Используется и в формах (для подсветки ошибок), и в репозиториях
// (как последний рубеж перед записью в файл).
public sealed class CustomerValidator : AbstractValidator<Customer>
{
    public CustomerValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Имя клиента обязательно")
            .MaximumLength(200).WithMessage("Имя не должно превышать 200 символов");

        // Email необязателен, но если указан - должен быть валидным.
        RuleFor(c => c.Email)
            .EmailAddress().WithMessage("Некорректный e-mail")
            .When(c => !string.IsNullOrWhiteSpace(c.Email));

        // Телефон необязателен; разрешаем цифры, +, скобки, дефисы, пробелы.
        RuleFor(c => c.Phone)
            .Matches(@"^[\d\+\(\)\-\s]+$").WithMessage("Некорректный номер телефона")
            .When(c => !string.IsNullOrWhiteSpace(c.Phone));

        // Для юр. лица обязательно название компании.
        RuleFor(c => c.CompanyName)
            .NotEmpty().WithMessage("Для юр. лица укажите название компании")
            .When(c => c.Type == CustomerType.Company);

        // ИНН только для юр. лиц, и если задан - 10 или 12 цифр.
        RuleFor(c => c.Inn)
            .Matches(@"^\d{10}$|^\d{12}$").WithMessage("ИНН должен содержать 10 или 12 цифр")
            .When(c => c.Type == CustomerType.Company && !string.IsNullOrWhiteSpace(c.Inn));

        // Дата рождения - не в будущем, и не ранее 1900 года.
        RuleFor(c => c.BirthDate)
            .Must(d => d == null || (d >= new DateOnly(1900, 1, 1) && d <= DateOnly.FromDateTime(DateTime.Today)))
            .WithMessage("Дата рождения некорректна");
    }
}
