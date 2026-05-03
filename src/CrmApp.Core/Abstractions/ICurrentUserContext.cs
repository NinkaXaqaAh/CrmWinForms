using CrmApp.Core.Models;

namespace CrmApp.Core.Abstractions;

// Singleton-держатель текущего вошедшего пользователя.
// Реализация (CurrentUserContext) живёт в WinForms.Composition.
public interface ICurrentUserContext
{
    User? Current { get; }
    bool IsAuthenticated { get; }
    void SetCurrent(User user);
    void Clear();
}
