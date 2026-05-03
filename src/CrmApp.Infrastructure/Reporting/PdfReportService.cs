using CrmApp.Core.Abstractions;
using CrmApp.Core.Enums;
using CrmApp.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CrmApp.Infrastructure.Reporting;

// PDF-отчёт по сделке. Цель — один лист A4.
//
// История правок:
//   v1: page.Header() + Row(RelativeItem.Element(c => Column(...))) → 4 страницы.
//   v2: Table с двумя RelativeColumn'ами → тоже 4 страницы. QuestPDF не умеет
//       обмерять Element-обёртки внутри Table.Cell, когда внутри Column с динамической высотой.
//   v3 (текущая): один сквозной Column. Никаких Row/Table для двухколоночного layout —
//       все секции стекаются вертикально. Контент маленький, в A4 умещается с запасом.
public sealed class PdfReportService : IPdfReportService
{
    static PdfReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task GenerateDealReportAsync(
        Deal deal,
        Customer? customer,
        IReadOnlyList<Activity> activities,
        string filePath,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(deal);
        ArgumentNullException.ThrowIfNull(activities);
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        var activitiesSnapshot = activities.ToList();

        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.2f, Unit.Centimetre);
                    page.DefaultTextStyle(s => s.FontSize(9).FontFamily("Segoe UI"));

                    page.Content().Column(col =>
                    {
                        col.Spacing(6);

                        // Заголовок отчёта.
                        col.Item().Text(t =>
                        {
                            t.Span("Отчёт по сделке: ").FontSize(13).Bold();
                            t.Span($"«{deal.Title}»").FontSize(13).Bold().FontColor(Colors.Blue.Darken2);
                        });
                        col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);

                        // Параметры сделки.
                        col.Item().PaddingTop(4).Text("Параметры сделки").FontSize(10).Bold().FontColor(Colors.Grey.Darken3);
                        AddKeyValueRows(col, new[]
                        {
                            ("Этап", LocalizeStage(deal.Stage)),
                            ("Сумма", deal.Amount.ToString()),
                            ("Вероятность", $"{deal.Probability}%"),
                            ("Ожидаемое закрытие", deal.ExpectedCloseDate?.ToString("dd.MM.yyyy") ?? "—"),
                            ("Фактическое закрытие", deal.ActualCloseDate?.ToString("dd.MM.yyyy") ?? "—"),
                            ("Создана", deal.CreatedAt.ToString("dd.MM.yyyy")),
                        });

                        // Клиент.
                        col.Item().PaddingTop(8).Text("Клиент").FontSize(10).Bold().FontColor(Colors.Grey.Darken3);
                        if (customer is null)
                        {
                            col.Item().Text("Клиент не привязан").Italic().FontSize(8).FontColor(Colors.Grey.Darken1);
                        }
                        else
                        {
                            AddKeyValueRows(col, new[]
                            {
                                ("Имя/Компания", customer.DisplayName),
                                ("Тип", LocalizeCustomerType(customer.Type)),
                                ("Статус", LocalizeCustomerStatus(customer.Status)),
                                ("ИНН", customer.Inn ?? "—"),
                                ("Телефон", customer.Phone ?? "—"),
                                ("Email", customer.Email ?? "—"),
                                ("Адрес", customer.Address ?? "—"),
                            });
                        }

                        // Описание (если задано).
                        if (!string.IsNullOrWhiteSpace(deal.Description))
                        {
                            col.Item().PaddingTop(8).Text("Описание").FontSize(10).Bold().FontColor(Colors.Grey.Darken3);
                            col.Item().Background(Colors.Grey.Lighten4).Padding(5).Text(deal.Description!).FontSize(8);
                        }

                        // Активности.
                        col.Item().PaddingTop(8).Text($"Связанные активности ({activitiesSnapshot.Count})")
                            .FontSize(10).Bold().FontColor(Colors.Grey.Darken3);

                        if (activitiesSnapshot.Count == 0)
                        {
                            col.Item().Text("Связанных активностей нет.").Italic().FontSize(8).FontColor(Colors.Grey.Darken1);
                        }
                        else
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(4);
                                    c.RelativeColumn(2);
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Срок").FontSize(8).Bold();
                                    h.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Тип").FontSize(8).Bold();
                                    h.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Заголовок").FontSize(8).Bold();
                                    h.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Статус").FontSize(8).Bold();
                                });

                                foreach (var a in activitiesSnapshot.OrderBy(x => x.DueDate))
                                {
                                    table.Cell().Padding(3).Text(a.DueDate.ToString("dd.MM.yyyy HH:mm")).FontSize(8);
                                    table.Cell().Padding(3).Text(LocalizeActivityType(a.Type)).FontSize(8);
                                    table.Cell().Padding(3).Text(a.Title).FontSize(8);
                                    table.Cell().Padding(3).Text(LocalizeActivityStatus(a.Status)).FontSize(8);
                                }
                            });
                        }
                    });

                    page.Footer().AlignRight().Text(t =>
                    {
                        t.Span("Стр. ").FontSize(7).FontColor(Colors.Grey.Darken1);
                        t.CurrentPageNumber().FontSize(7);
                        t.Span(" / ").FontSize(7).FontColor(Colors.Grey.Darken1);
                        t.TotalPages().FontSize(7);
                    });
                });
            });

            document.GeneratePdf(filePath);
        }, ct);
    }

    // Каждая пара "ключ — значение" — отдельный Item в общем Column'е документа.
    // Внутри Item используется Row с константной шириной ключа: легко обмеряется,
    // не разваливает страницу как вложенный Column в Row.
    private static void AddKeyValueRows(ColumnDescriptor col, IReadOnlyList<(string key, string value)> rows)
    {
        foreach (var (key, value) in rows)
        {
            col.Item().Row(row =>
            {
                row.ConstantItem(140).Text(key).FontSize(8).FontColor(Colors.Grey.Darken2);
                row.RelativeItem().Text(value).FontSize(8);
            });
        }
    }

    private static string LocalizeStage(DealStage s) => s switch
    {
        DealStage.New => "Новая",
        DealStage.Qualified => "Квалификация",
        DealStage.Proposal => "Предложение",
        DealStage.Negotiation => "Переговоры",
        DealStage.Won => "Выиграна",
        DealStage.Lost => "Проиграна",
        _ => s.ToString(),
    };

    private static string LocalizeCustomerType(CustomerType t) => t switch
    {
        CustomerType.Person => "Физлицо",
        CustomerType.Company => "Компания",
        _ => t.ToString(),
    };

    private static string LocalizeCustomerStatus(CustomerStatus s) => s switch
    {
        CustomerStatus.Lead => "Лид",
        CustomerStatus.Active => "Активный",
        CustomerStatus.Vip => "VIP",
        CustomerStatus.Inactive => "Неактивный",
        CustomerStatus.Blocked => "Заблокирован",
        _ => s.ToString(),
    };

    private static string LocalizeActivityType(ActivityType t) => t switch
    {
        ActivityType.Call => "Звонок",
        ActivityType.Meeting => "Встреча",
        ActivityType.Email => "Письмо",
        ActivityType.Task => "Задача",
        _ => t.ToString(),
    };

    private static string LocalizeActivityStatus(ActivityStatus s) => s switch
    {
        ActivityStatus.Planned => "Запланирована",
        ActivityStatus.InProgress => "В работе",
        ActivityStatus.Completed => "Завершена",
        ActivityStatus.Cancelled => "Отменена",
        _ => s.ToString(),
    };
}
