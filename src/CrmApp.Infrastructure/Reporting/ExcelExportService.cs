using ClosedXML.Excel;
using CrmApp.Core.Abstractions;
using CrmApp.Core.Common;

namespace CrmApp.Infrastructure.Reporting;

// Реализация выгрузки в Excel поверх ClosedXML.
// ClosedXML.SaveAs синхронный — оборачиваем в Task.Run, чтобы не блокировать UI-поток.
public sealed class ExcelExportService : IExcelExportService
{
    public Task ExportAsync(
        string sheetName,
        IReadOnlyList<string> headers,
        IEnumerable<IReadOnlyList<object?>> rows,
        string filePath,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sheetName);
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        // Материализуем строки до ухода в Task.Run, чтобы LINQ-замыкания формы
        // не вычислялись из фонового потока (BindingList не потокобезопасен).
        var materialized = rows.Select(r => r.ToArray()).ToList();

        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            using var wb = new XLWorkbook();

            // Имя листа в Excel ограничено 31 символом и не может содержать /\?*[]:
            var safeSheet = SanitizeSheetName(sheetName);
            var ws = wb.Worksheets.Add(safeSheet);

            // Шапка: жирным, на сером фоне, с автофильтром.
            for (var i = 0; i < headers.Count; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
            }
            var headerRange = ws.Range(1, 1, 1, headers.Count);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            // Данные.
            var rowIndex = 2;
            foreach (var row in materialized)
            {
                ct.ThrowIfCancellationRequested();
                for (var c = 0; c < row.Length; c++)
                {
                    SetCellValue(ws.Cell(rowIndex, c + 1), row[c]);
                }
                rowIndex++;
            }

            // Автофильтр и автоширина колонок — стандартная "удобная" Excel-выгрузка.
            if (rowIndex > 2)
            {
                ws.Range(1, 1, rowIndex - 1, headers.Count).SetAutoFilter();
            }
            ws.Columns().AdjustToContents();

            wb.SaveAs(filePath);
        }, ct);
    }

    // Кладёт значение в ячейку с подходящим Excel-типом (число, дата, строка).
    // Это важно для сортировки и фильтрации в самом Excel — текстовые "1 250 000 ₽"
    // отсортируются лексикографически и пользователь будет ругаться.
    private static void SetCellValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                cell.Value = string.Empty;
                break;
            case string s:
                cell.Value = s;
                break;
            case bool b:
                cell.Value = b ? "Да" : "Нет";
                break;
            case decimal d:
                cell.Value = d;
                cell.Style.NumberFormat.Format = "#,##0.00";
                break;
            case double dbl:
                cell.Value = dbl;
                cell.Style.NumberFormat.Format = "#,##0.00";
                break;
            case int i:
                cell.Value = i;
                break;
            case DateTime dt:
                cell.Value = dt;
                cell.Style.NumberFormat.Format = "dd.mm.yyyy hh:mm";
                break;
            case DateOnly date:
                cell.Value = date.ToDateTime(TimeOnly.MinValue);
                cell.Style.NumberFormat.Format = "dd.mm.yyyy";
                break;
            case Money money:
                // Сумма как число + валюта в записи формата (отображение "1 250 000,00 ₽").
                cell.Value = money.Amount;
                cell.Style.NumberFormat.Format = money.Currency switch
                {
                    "RUB" => "#,##0.00 \"₽\"",
                    "USD" => "$#,##0.00",
                    "EUR" => "€#,##0.00",
                    _ => $"#,##0.00 \"{money.Currency}\"",
                };
                break;
            default:
                cell.Value = value.ToString() ?? string.Empty;
                break;
        }
    }

    private static string SanitizeSheetName(string name)
    {
        var clean = name;
        foreach (var c in new[] { '\\', '/', '?', '*', '[', ']', ':' })
        {
            clean = clean.Replace(c, '_');
        }
        return clean.Length > 31 ? clean[..31] : clean;
    }
}
