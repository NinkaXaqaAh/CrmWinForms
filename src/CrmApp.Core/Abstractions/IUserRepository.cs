using CrmApp.Core.Models;

namespace CrmApp.Core.Abstractions;

public interface IUserRepository : IRepository<User>
{
    // Поиск по логину без учёта регистра. Возвращает null если нет.
    Task<User?> FindByLoginAsync(string login, CancellationToken ct = default);
}
