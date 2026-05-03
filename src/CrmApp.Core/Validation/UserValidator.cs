using CrmApp.Core.Models;
using FluentValidation;

namespace CrmApp.Core.Validation;

public sealed class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(u => u.Login)
            .NotEmpty().WithMessage("Логин обязателен")
            .MinimumLength(3).WithMessage("Логин должен быть не короче 3 символов")
            .MaximumLength(50)
            // Допускаем кириллицу, латиницу, цифры и разделители: _ . -
            .Matches(@"^[A-Za-zА-Яа-яЁё0-9_\.\-]+$")
            .WithMessage("Логин может содержать буквы (русские или латинские), цифры, _, ., -");

        RuleFor(u => u.FullName)
            .NotEmpty().WithMessage("ФИО обязательно")
            .MaximumLength(200);

        RuleFor(u => u.Email)
            .EmailAddress().WithMessage("Некорректный e-mail")
            .When(u => !string.IsNullOrWhiteSpace(u.Email));

        RuleFor(u => u.PasswordHash)
            .NotEmpty().WithMessage("Пароль не задан");
    }
}
