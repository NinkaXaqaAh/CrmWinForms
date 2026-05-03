namespace CrmApp.Core.Abstractions;

// Универсальный сервис выгрузки табличных данных в Excel-файл.
// Намеренно generic-агностик: каждая форма-список знает свои колонки и преобразует
// сущности в строки object[]. Сервису передают уже подготовленные данные, без знания о моделях.
//
// Это позволяет инфраструктурному слою не зависеть от UI-локализации (ToRussian),
// а UI-слою — не тащить ClosedXML.
public interface IExcelExportService
{
    Task ExportAsync(
        string sheetName,
        IReadOnlyList<string> headers,
        IEnumerable<IReadOnlyList<object?>> rows,
        string filePath,
        CancellationToken ct = default);
}
