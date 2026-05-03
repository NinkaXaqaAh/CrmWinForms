using CrmApp.Core.Abstractions;
using CrmApp.Core.Common;
using CrmApp.Core.Enums;
using CrmApp.Core.Models;
using CrmApp.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CrmApp.Infrastructure.Persistence.Json;

public sealed class JsonActivityRepository : JsonRepository<Activity>, IActivityRepository
{
    private readonly IDateTimeProvider _clock;

    public JsonActivityRepository(
        IOptions<JsonStorageOptions> options,
        IDateTimeProvider clock,
        ILogger<JsonActivityRepository> logger)
        : base("activities.json", options, clock, logger)
    {
        _clock = clock;
    }

    public async Task<IReadOnlyList<Activity>> GetByCustomerAsync(
        Guid customerId, CancellationToken ct = default)
    {
        var items = await SnapshotAsync(ct).ConfigureAwait(false);
        return items.Where(a => a.CustomerId == customerId).ToList();
    }

    public async Task<IReadOnlyList<Activity>> GetByDealAsync(
        Guid dealId, CancellationToken ct = default)
    {
        var items = await SnapshotAsync(ct).ConfigureAwait(false);
        return items.Where(a => a.DealId == dealId).ToList();
    }

    public async Task<IReadOnlyList<Activity>> GetUpcomingAsync(
        int days, CancellationToken ct = default)
    {
        var items = await SnapshotAsync(ct).ConfigureAwait(false);
        var now = _clock.Now;
        var until = now.AddDays(days);

        return items.Where(a =>
            a.Status is ActivityStatus.Planned or ActivityStatus.InProgress &&
            a.DueDate >= now && a.DueDate <= until)
            .OrderBy(a => a.DueDate)
            .ToList();
    }

    public async Task<IReadOnlyList<Activity>> GetOverdueAsync(CancellationToken ct = default)
    {
        var items = await SnapshotAsync(ct).ConfigureAwait(false);
        var now = _clock.Now;
        return items.Where(a => a.IsOverdue(now))
            .OrderBy(a => a.DueDate)
            .ToList();
    }
}
