using CrmApp.Core.Models;

namespace CrmApp.Core.Abstractions;

// Сервис генерации PDF-отчётов.
// Принимает уже подготовленные доменные сущности и сохраняет файл по указанному пути.
public interface IPdfReportService
{
    // Отчёт по сделке: реквизиты, клиент, описание, связанные активности.
    Task GenerateDealReportAsync(
        Deal deal,
        Customer? customer,
        IReadOnlyList<Activity> activities,
        string filePath,
        CancellationToken ct = default);
}
