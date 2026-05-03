namespace CrmApp.WinForms.Search;

// Сервис глобального поиска — агрегирует результаты по 4 сущностям.
// Живёт в UI-слое, потому что использует презентационную локализацию (ToRussian).
// При расширении до отдельного API можно вынести в Core с DTO без UI-зависимостей.
public interface ISearchService
{
    Task<IReadOnlyList<SearchHit>> SearchAsync(string query, CancellationToken ct = default);
}
