namespace CrmApp.Core.Exceptions;

// Исключение для случая "сущности с таким Id нет".
// Кидается из репозиториев в GetByIdAsync, UpdateAsync, DeleteAsync — там, где отсутствие
// записи означает явную ошибку (не пустой список, а именно "не нашли по ключу").
public sealed class EntityNotFoundException : Exception
{
    public string EntityName { get; }
    public Guid EntityId { get; }

    public EntityNotFoundException(string entityName, Guid entityId)
        : base($"Сущность {entityName} с Id={entityId} не найдена")
    {
        EntityName = entityName;
        EntityId = entityId;
    }
}
