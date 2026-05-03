using CrmApp.Core.Abstractions;
using CrmApp.Core.Common;

namespace CrmApp.Core.Models;

// Товар или услуга в каталоге малого бизнеса.
// Связи со сделками здесь нет: для MVP достаточно простого справочника.
// Если понадобится — на этапе 4 добавим DealItem (М2М с количеством).
public sealed class Product : IEntity, IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Description { get; set; }
    public Money Price { get; set; } = Money.Zero();
    public string? Category { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
