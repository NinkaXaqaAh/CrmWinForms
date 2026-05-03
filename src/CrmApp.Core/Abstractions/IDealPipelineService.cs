using CrmApp.Core.Common;
using CrmApp.Core.Enums;
using CrmApp.Core.Models;

namespace CrmApp.Core.Abstractions;

// Доменный сервис воронки сделок.
// Знает про допустимые переходы по этапам и считает прогноз выручки.
public interface IDealPipelineService
{
    // Перевод на новый этап с проверкой допустимости перехода.
    // Идемпотентен: повторный вызов на тот же этап ничего не меняет.
    // Кидает DomainException при недопустимом переходе.
    Task MoveToStageAsync(Guid dealId, DealStage newStage, CancellationToken ct = default);

    // Взвешенный прогноз: сумма (Amount * Probability / 100) по сделкам, закрывающимся в периоде.
    Task<Money> ForecastRevenueAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct = default);

    // Сделки, сгруппированные по этапу (для будущей kanban-доски).
    Task<IReadOnlyDictionary<DealStage, IReadOnlyList<Deal>>> GetPipelineAsync(CancellationToken ct = default);

    // Конверсия каждого этапа: % сделок, переходящих на следующий.
    Task<IReadOnlyDictionary<DealStage, double>> GetConversionRatesAsync(CancellationToken ct = default);
}
