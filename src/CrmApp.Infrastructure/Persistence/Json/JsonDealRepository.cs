using CrmApp.Core.Abstractions;
using CrmApp.Core.Common;
using CrmApp.Core.Enums;
using CrmApp.Core.Models;
using CrmApp.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CrmApp.Infrastructure.Persistence.Json;

public sealed class JsonDealRepository : JsonRepository<Deal>, IDealRepository
{
    public JsonDealRepository(
        IOptions<JsonStorageOptions> options,
        IDateTimeProvider clock,
        ILogger<JsonDealRepository> logger)
        : base("deals.json", options, clock, logger)
    {
    }

    public async Task<IReadOnlyList<Deal>> GetByCustomerAsync(
        Guid customerId, CancellationToken ct = default)
    {
        var items = await SnapshotAsync(ct).ConfigureAwait(false);
        return items.Where(d => d.CustomerId == customerId).ToList();
    }

    public async Task<IReadOnlyList<Deal>> GetByStageAsync(
        DealStage stage, CancellationToken ct = default)
    {
        var items = await SnapshotAsync(ct).ConfigureAwait(false);
        return items.Where(d => d.Stage == stage).ToList();
    }

    public async Task<IReadOnlyList<Deal>> GetClosingInPeriodAsync(
        DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
    {
        var items = await SnapshotAsync(ct).ConfigureAwait(false);
        return items.Where(d =>
            d.ExpectedCloseDate.HasValue &&
            d.ExpectedCloseDate.Value >= startDate &&
            d.ExpectedCloseDate.Value <= endDate).ToList();
    }
}
