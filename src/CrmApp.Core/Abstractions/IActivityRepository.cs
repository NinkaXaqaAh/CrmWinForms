using CrmApp.Core.Models;

namespace CrmApp.Core.Abstractions;

public interface IActivityRepository : IRepository<Activity>
{
    Task<IReadOnlyList<Activity>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<Activity>> GetByDealAsync(Guid dealId, CancellationToken ct = default);

    // Активности на ближайшие N дней (для виджета "Задачи на неделю").
    Task<IReadOnlyList<Activity>> GetUpcomingAsync(int days, CancellationToken ct = default);
    Task<IReadOnlyList<Activity>> GetOverdueAsync(CancellationToken ct = default);
}
