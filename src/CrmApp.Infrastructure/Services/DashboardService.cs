using CrmApp.Core.Abstractions;
using CrmApp.Core.Common;
using CrmApp.Core.Enums;
using CrmApp.Core.Models;

namespace CrmApp.Infrastructure.Services;

// Агрегатор метрик главного экрана.
public sealed class DashboardService : IDashboardService
{
    private readonly ICustomerRepository _customers;
    private readonly IDealRepository _deals;
    private readonly IActivityRepository _activities;
    private readonly IDealPipelineService _pipeline;
    private readonly IDateTimeProvider _clock;

    public DashboardService(
        ICustomerRepository customers,
        IDealRepository deals,
        IActivityRepository activities,
        IDealPipelineService pipeline,
        IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(customers);
        ArgumentNullException.ThrowIfNull(deals);
        ArgumentNullException.ThrowIfNull(activities);
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(clock);

        _customers = customers;
        _deals = deals;
        _activities = activities;
        _pipeline = pipeline;
        _clock = clock;
    }

    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        var customers = await _customers.GetAllAsync(ct).ConfigureAwait(false);
        var deals = await _deals.GetAllAsync(ct).ConfigureAwait(false);
        var overdue = await _activities.GetOverdueAsync(ct).ConfigureAwait(false);
        var upcoming = await _activities.GetUpcomingAsync(7, ct).ConfigureAwait(false);

        var won = deals.Where(d => d.Stage == DealStage.Won).ToList();
        var wonRevenue = SumOrZero(won);

        var today = _clock.Today;
        var endOfQuarter = today.AddMonths(3);
        var forecast = await _pipeline.ForecastRevenueAsync(today, endOfQuarter, ct).ConfigureAwait(false);

        return new DashboardSummary(
            TotalCustomers: customers.Count,
            ActiveDeals: deals.Count(d => !d.IsClosed),
            TotalWonRevenue: wonRevenue,
            ForecastRevenue: forecast,
            OverdueActivities: overdue.Count,
            UpcomingActivities: upcoming.Count);
    }

    public async Task<IReadOnlyList<MonthlyRevenuePoint>> GetMonthlyRevenueAsync(
        int months, CancellationToken ct = default)
    {
        if (months <= 0) months = 6;

        var deals = await _deals.GetAllAsync(ct).ConfigureAwait(false);
        var won = deals.Where(d => d.Stage == DealStage.Won && d.ActualCloseDate.HasValue).ToList();

        var today = _clock.Today;
        var points = new List<MonthlyRevenuePoint>(months);

        // Идём от старшего месяца к текущему.
        for (var i = months - 1; i >= 0; i--)
        {
            var month = today.AddMonths(-i);
            var inMonth = won.Where(d =>
                d.ActualCloseDate!.Value.Year == month.Year &&
                d.ActualCloseDate!.Value.Month == month.Month).ToList();

            points.Add(new MonthlyRevenuePoint(month.Year, month.Month, SumOrZero(inMonth)));
        }

        return points;
    }

    public async Task<IReadOnlyDictionary<CustomerStatus, int>> GetCustomersByStatusAsync(
        CancellationToken ct = default)
    {
        var customers = await _customers.GetAllAsync(ct).ConfigureAwait(false);

        var result = new Dictionary<CustomerStatus, int>();
        foreach (var status in Enum.GetValues<CustomerStatus>())
        {
            result[status] = customers.Count(c => c.Status == status);
        }
        return result;
    }

    private static Money SumOrZero(IReadOnlyCollection<Deal> deals)
    {
        if (deals.Count == 0) return Money.Zero();

        var currency = deals.First().Amount.Currency;
        var total = deals.Sum(d => d.Amount.Amount);
        return new Money(total, currency);
    }
}
