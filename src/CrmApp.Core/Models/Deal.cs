using CrmApp.Core.Abstractions;
using CrmApp.Core.Common;
using CrmApp.Core.Enums;

namespace CrmApp.Core.Models;

// Сделка — единица воронки продаж.
// Probability и ActualCloseDate автоматически синхронизируются с этапом
// в DealPipelineService.MoveToStageAsync, но также доступны для ручной правки в форме.
public sealed class Deal : IEntity, IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DealStage Stage { get; set; } = DealStage.New;

    // Сумма с валютой; default — 0 ₽.
    public Money Amount { get; set; } = Money.Zero();

    // Вероятность закрытия в % (0..100). Используется в weighted-прогнозе.
    public int Probability { get; set; }

    public Guid CustomerId { get; set; }
    public Guid? AssignedUserId { get; set; }

    public DateOnly? ExpectedCloseDate { get; set; }
    public DateOnly? ActualCloseDate { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Терминальные этапы — закрытая сделка.
    public bool IsClosed => Stage is DealStage.Won or DealStage.Lost;
}
