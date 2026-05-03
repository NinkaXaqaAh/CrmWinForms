using CrmApp.Core.Abstractions;
using CrmApp.Core.Models;

namespace CrmApp.WinForms.Composition;

// Singleton-держатель текущего вошедшего пользователя.
// Заполняется LoginForm после успешной аутентификации, читается остальными формами.
public sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly Lock _lock = new();
    private User? _current;

    public User? Current
    {
        get
        {
            lock (_lock) return _current;
        }
    }

    public bool IsAuthenticated => Current is not null;

    public void SetCurrent(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        lock (_lock) _current = user;
    }

    public void Clear()
    {
        lock (_lock) _current = null;
    }
}
