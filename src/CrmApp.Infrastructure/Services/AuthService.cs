using System.Security.Cryptography;
using CrmApp.Core.Abstractions;
using CrmApp.Core.Common;
using CrmApp.Core.Models;
using Microsoft.Extensions.Logging;

namespace CrmApp.Infrastructure.Services;

// Сервис аутентификации с PBKDF2-хешированием паролей.
// PBKDF2 - стандартный безопасный KDF, доступный в .NET без дополнительных пакетов.
// 100 000 итераций SHA256 + 16-байтовая соль на пароль.
public sealed class AuthService : IAuthService
{
    // Параметры PBKDF2: соль 16 байт, ключ 32 байта, итераций 100000.
    // Хеш в формате: <iterations>.<base64salt>.<base64hash>
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    private readonly IUserRepository _users;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IDateTimeProvider clock,
        ILogger<AuthService> logger)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);

        _users = users;
        _clock = clock;
        _logger = logger;
    }

    public async Task<User?> AuthenticateAsync(
        string login, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        var user = await _users.FindByLoginAsync(login, ct).ConfigureAwait(false);
        if (user is null || !user.IsActive)
        {
            // Не разделяем "нет пользователя" и "неверный пароль" - защита от перебора.
            _logger.LogWarning("Неудачная попытка входа: {Login}", login);
            return null;
        }

        if (!VerifyPassword(password, user.PasswordHash))
        {
            _logger.LogWarning("Неверный пароль для {Login}", login);
            return null;
        }

        // Обновляем дату последнего входа.
        user.LastLoginAt = _clock.Now;
        await _users.UpdateAsync(user, ct).ConfigureAwait(false);

        _logger.LogInformation("Успешный вход: {Login} (роль {Role})", user.Login, user.Role);
        return user;
    }

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash)) return false;

        var parts = hash.Split('.', 3);
        if (parts.Length != 3) return false;

        if (!int.TryParse(parts[0], out var iterations)) return false;

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expected = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expected.Length);

        // Сравнение в постоянное время - защита от time-based атак.
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
