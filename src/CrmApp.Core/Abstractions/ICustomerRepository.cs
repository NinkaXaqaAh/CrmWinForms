using CrmApp.Core.Enums;
using CrmApp.Core.Models;

namespace CrmApp.Core.Abstractions;

// Репозиторий клиентов — базовый CRUD + специфичные запросы.
public interface ICustomerRepository : IRepository<Customer>
{
    Task<IReadOnlyList<Customer>> GetByStatusAsync(CustomerStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> GetByAssignedUserAsync(Guid userId, CancellationToken ct = default);

    // Поиск по имени, компании, телефону, email, ИНН — без учёта регистра.
    Task<IReadOnlyList<Customer>> SearchAsync(string searchText, CancellationToken ct = default);
}
