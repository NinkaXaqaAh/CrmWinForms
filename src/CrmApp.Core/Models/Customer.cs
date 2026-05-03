using CrmApp.Core.Abstractions;
using CrmApp.Core.Enums;

namespace CrmApp.Core.Models;

// Клиент CRM. Может быть физлицом (Person) или юрлицом (Company).
// CompanyName/Inn/Position имеют смысл только для Company; BirthDate — только для Person.
// Эта инвариантность контролируется в CustomerValidator и в форме (UpdateTypeSpecificFields).
public sealed class Customer : IEntity, IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public CustomerType Type { get; set; } = CustomerType.Person;
    public CustomerStatus Status { get; set; } = CustomerStatus.Lead;

    // Контактное лицо: ФИО для Person; ФИО представителя для Company.
    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }

    // Поля юрлица.
    public string? CompanyName { get; set; }
    public string? Inn { get; set; }
    public string? Position { get; set; }

    // Поля физлица.
    public DateOnly? BirthDate { get; set; }

    // Менеджер, ведущий клиента.
    public Guid? AssignedUserId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Имя для отображения в списках. Для компании — название компании,
    // для физлица — имя контакта.
    public string DisplayName =>
        Type == CustomerType.Company && !string.IsNullOrWhiteSpace(CompanyName)
            ? CompanyName
            : Name;
}
