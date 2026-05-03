using CrmApp.Core.Abstractions;
using CrmApp.Core.Enums;

namespace CrmApp.Core.Models;

// Взаимодействие с клиентом или внутренняя задача.
// Может быть привязана к клиенту, к сделке, либо ни к чему (внутренняя задача менеджера).
public sealed class Activity : IEntity, IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ActivityType Type { get; set; } = ActivityType.Task;
    public ActivityStatus Status { get; set; } = ActivityStatus.Planned;
    public Priority Priority { get; set; } = Priority.Normal;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid? CustomerId { get; set; }
    public Guid? DealId { get; set; }
    public Guid? AssignedUserId { get; set; }

    public DateTime DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // "Просрочена" = не завершена и не отменена, и срок прошёл.
    // Принимаем "сейчас" параметром — чтобы тесты могли подставить фиксированную дату.
    public bool IsOverdue(DateTime now) =>
        Status is not ActivityStatus.Completed and not ActivityStatus.Cancelled
        && DueDate < now;
}
