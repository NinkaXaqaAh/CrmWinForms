using CrmApp.Core.Common;

namespace CrmApp.Core.Models;

// Снимок ключевых метрик для главного экрана.
// Record для удобной сериализации и неизменяемости — этот объект не редактируется UI'ем.
public sealed record DashboardSummary(
    int TotalCustomers,
    int ActiveDeals,
    Money TotalWonRevenue,
    Money ForecastRevenue,
    int OverdueActivities,
    int UpcomingActivities);
