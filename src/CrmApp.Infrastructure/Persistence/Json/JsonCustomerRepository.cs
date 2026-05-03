using CrmApp.Core.Abstractions;
using CrmApp.Core.Common;
using CrmApp.Core.Enums;
using CrmApp.Core.Models;
using CrmApp.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CrmApp.Infrastructure.Persistence.Json;

public sealed class JsonCustomerRepository : JsonRepository<Customer>, ICustomerRepository
{
    public JsonCustomerRepository(
        IOptions<JsonStorageOptions> options,
        IDateTimeProvider clock,
        ILogger<JsonCustomerRepository> logger)
        : base("customers.json", options, clock, logger)
    {
    }

    public async Task<IReadOnlyList<Customer>> GetByStatusAsync(
        CustomerStatus status, CancellationToken ct = default)
    {
        var items = await SnapshotAsync(ct).ConfigureAwait(false);
        return items.Where(c => c.Status == status).ToList();
    }

    public async Task<IReadOnlyList<Customer>> GetByAssignedUserAsync(
        Guid userId, CancellationToken ct = default)
    {
        var items = await SnapshotAsync(ct).ConfigureAwait(false);
        return items.Where(c => c.AssignedUserId == userId).ToList();
    }

    public async Task<IReadOnlyList<Customer>> SearchAsync(
        string searchText, CancellationToken ct = default)
    {
        var items = await SnapshotAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return items;
        }

        var s = searchText.Trim();
        return items.Where(c =>
            (c.Name?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (c.CompanyName?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (c.Phone?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (c.Email?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (c.Inn?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();
    }
}
