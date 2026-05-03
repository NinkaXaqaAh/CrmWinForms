using CrmApp.Core.Enums;
using CrmApp.Core.Models;

namespace CrmApp.Core.Abstractions;

public interface IDealRepository : IRepository<Deal>
{
    Task<IReadOnlyList<Deal>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<Deal>> GetByStageAsync(DealStage stage, CancellationToken ct = default);

    // Используется в прогнозе выручки — сделки с ожидаемой датой закрытия в диапазоне.
    Task<IReadOnlyList<Deal>> GetClosingInPeriodAsync(
        DateOnly startDate, DateOnly endDate, CancellationToken ct = default);
}
