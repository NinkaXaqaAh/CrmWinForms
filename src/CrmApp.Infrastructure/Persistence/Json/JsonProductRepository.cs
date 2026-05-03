using CrmApp.Core.Abstractions;
using CrmApp.Core.Models;
using CrmApp.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CrmApp.Infrastructure.Persistence.Json;

public sealed class JsonProductRepository : JsonRepository<Product>, IProductRepository
{
    public JsonProductRepository(
        IOptions<JsonStorageOptions> options,
        IDateTimeProvider clock,
        ILogger<JsonProductRepository> logger)
        : base("products.json", options, clock, logger)
    {
    }

    public async Task<IReadOnlyList<Product>> GetActiveAsync(CancellationToken ct = default)
    {
        var items = await SnapshotAsync(ct).ConfigureAwait(false);
        return items.Where(p => p.IsActive).ToList();
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(string searchText, CancellationToken ct = default)
    {
        var items = await SnapshotAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return items;
        }

        var s = searchText.Trim();
        return items.Where(p =>
            (p.Name?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (p.Sku?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (p.Category?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();
    }
}
