namespace CrmApp.Core.Abstractions;

// Базовый асинхронный репозиторий доменной сущности.
// Все операции принимают CancellationToken — это обязательный default для async I/O в .NET.
// FindByIdAsync возвращает null при отсутствии; GetByIdAsync кидает EntityNotFoundException.
public interface IRepository<T> where T : class, IEntity, IAuditable
{
    Task<T> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<T?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}
