namespace CrmApp.WinForms.Search;

// Тип найденного результата — определяет, какой список открывать после двойного клика.
public enum SearchHitKind
{
    Customer,
    Deal,
    Activity,
    Product,
}

// Один результат глобального поиска. Title — основное название (имя клиента, заголовок сделки),
// Subtitle — вспомогательная информация (статус, сумма, срок и т.п.).
public sealed record SearchHit(
    SearchHitKind Kind,
    Guid EntityId,
    string Title,
    string Subtitle);
