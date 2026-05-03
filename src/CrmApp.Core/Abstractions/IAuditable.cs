namespace CrmApp.Core.Abstractions;

// Метаданные аудита: даты создания и последнего обновления.
// Заполняются репозиторием автоматически — UI не должен их трогать.
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
