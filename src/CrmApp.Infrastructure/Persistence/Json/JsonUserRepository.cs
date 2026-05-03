using CrmApp.Core.Abstractions;
using CrmApp.Core.Common;
using CrmApp.Core.Models;
using CrmApp.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CrmApp.Infrastructure.Persistence.Json;

public sealed class JsonUserRepository : JsonRepository<User>, IUserRepository
{
    public JsonUserRepository(
        IOptions<JsonStorageOptions> options,
        IDateTimeProvider clock,
        ILogger<JsonUserRepository> logger)
        : base("users.json", options, clock, logger)
    {
    }

    public async Task<User?> FindByLoginAsync(string login, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(login)) return null;

        var items = await SnapshotAsync(ct).ConfigureAwait(false);
        return items.FirstOrDefault(u =>
            string.Equals(u.Login, login, StringComparison.OrdinalIgnoreCase));
    }
}
