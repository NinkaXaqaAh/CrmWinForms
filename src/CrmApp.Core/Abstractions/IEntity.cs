namespace CrmApp.Core.Abstractions;

// Базовая абстракция доменной сущности — есть Id типа Guid.
// Setter оставлен для удобства: при загрузке из JSON System.Text.Json пишет в setter.
public interface IEntity
{
    Guid Id { get; set; }
}
