using CrmApp.Core.Models;

namespace CrmApp.Core.Abstractions;

public interface IAuthService
{
    // Возвращает пользователя при успехе, null — при провале.
    // Намеренно не разделяем "нет такого логина" и "неверный пароль" — защита от перебора.
    Task<User?> AuthenticateAsync(string login, string password, CancellationToken ct = default);

    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
