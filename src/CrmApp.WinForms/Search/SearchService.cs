using CrmApp.Core.Abstractions;
using CrmApp.WinForms.Localization;

namespace CrmApp.WinForms.Search;

// Реализация глобального поиска: запускает Search/GetAll по 4 репозиториям и
// собирает совпадения в единый список SearchHit.
public sealed class SearchService : ISearchService
{
    private readonly ICustomerRepository _customers;
    private readonly IDealRepository _deals;
    private readonly IActivityRepository _activities;
    private readonly IProductRepository _products;

    public SearchService(
        ICustomerRepository customers,
        IDealRepository deals,
        IActivityRepository activities,
        IProductRepository products)
    {
        ArgumentNullException.ThrowIfNull(customers);
        ArgumentNullException.ThrowIfNull(deals);
        ArgumentNullException.ThrowIfNull(activities);
        ArgumentNullException.ThrowIfNull(products);

        _customers = customers;
        _deals = deals;
        _activities = activities;
        _products = products;
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<SearchHit>();
        var q = query.Trim();

        var hits = new List<SearchHit>();

        // Клиенты — у репозитория уже есть SearchAsync (имя/компания/телефон/email/ИНН).
        var customerMatches = await _customers.SearchAsync(q, ct).ConfigureAwait(false);
        foreach (var c in customerMatches)
        {
            hits.Add(new SearchHit(
                SearchHitKind.Customer,
                c.Id,
                c.DisplayName,
                $"{c.Type.ToRussian()} • {c.Status.ToRussian()}"));
        }

        // Сделки — фильтруем вручную по названию/описанию.
        var allDeals = await _deals.GetAllAsync(ct).ConfigureAwait(false);
        foreach (var d in allDeals.Where(d =>
            d.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            (d.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)))
        {
            hits.Add(new SearchHit(
                SearchHitKind.Deal,
                d.Id,
                d.Title,
                $"{d.Stage.ToRussian()} • {d.Amount}"));
        }

        // Активности — фильтр по заголовку/описанию.
        var allActivities = await _activities.GetAllAsync(ct).ConfigureAwait(false);
        foreach (var a in allActivities.Where(a =>
            a.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            (a.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)))
        {
            hits.Add(new SearchHit(
                SearchHitKind.Activity,
                a.Id,
                a.Title,
                $"{a.Status.ToRussian()} • до {a.DueDate:dd.MM.yyyy}"));
        }

        // Товары — у репозитория тоже есть SearchAsync (название/SKU/категория).
        var productMatches = await _products.SearchAsync(q, ct).ConfigureAwait(false);
        foreach (var p in productMatches)
        {
            var status = p.IsActive ? "активен" : "архив";
            hits.Add(new SearchHit(
                SearchHitKind.Product,
                p.Id,
                p.Name,
                $"{p.Price} • {status}"));
        }

        return hits;
    }
}
