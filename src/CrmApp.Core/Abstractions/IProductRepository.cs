using CrmApp.Core.Models;

namespace CrmApp.Core.Abstractions;

public interface IProductRepository : IRepository<Product>
{
    // Только активные товары — для подстановки в формы сделок (когда привяжем).
    Task<IReadOnlyList<Product>> GetActiveAsync(CancellationToken ct = default);

    // Поиск по названию, SKU, категории — без учёта регистра.
    Task<IReadOnlyList<Product>> SearchAsync(string searchText, CancellationToken ct = default);
}
