using CrmApp.Core.Enums;
using CrmApp.Core.Models;

namespace CrmApp.Core.Abstractions;

// Агрегатор метрик главного экрана.
public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default);

    // Точки для графика "выручка по месяцам", N последних месяцев.
    Task<IReadOnlyList<MonthlyRevenuePoint>> GetMonthlyRevenueAsync(int months, CancellationToken ct = default);

    // Распределение клиентов по статусам — для круговой диаграммы.
    Task<IReadOnlyDictionary<CustomerStatus, int>> GetCustomersByStatusAsync(CancellationToken ct = default);
}
