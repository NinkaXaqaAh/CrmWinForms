using CrmApp.Core.Enums;

namespace CrmApp.WinForms.Localization;

// Единая точка перевода доменных enum'ов на русский для отображения в UI.
// Раньше каждая форма (Dashboard, DealList, ActivityList, ActivityEdit, DealEdit)
// держала свой private static LocalizeXxx — текст разъезжался при правках.
// Теперь все обращаются к extension-методам ToRussian() из этого файла.
//
// Display-строки сознательно не положены в Core, потому что это presentation-concern;
// если завтра появится английский UI — добавим вторую раскладку через ресурсы или DI.
public static class EnumLocalization
{
    public static string ToRussian(this CustomerType t) => t switch
    {
        CustomerType.Person => "Физлицо",
        CustomerType.Company => "Компания",
        _ => t.ToString(),
    };

    public static string ToRussian(this CustomerStatus s) => s switch
    {
        CustomerStatus.Lead => "Лид",
        CustomerStatus.Active => "Активный",
        // Vip оставляем как "VIP" по просьбе пользователя — переводы вроде "ВИП-клиент"
        // в русскоязычном CRM-обиходе не используются.
        CustomerStatus.Vip => "VIP",
        CustomerStatus.Inactive => "Неактивный",
        CustomerStatus.Blocked => "Заблокирован",
        _ => s.ToString(),
    };

    public static string ToRussian(this DealStage s) => s switch
    {
        DealStage.New => "Новая",
        DealStage.Qualified => "Квалификация",
        DealStage.Proposal => "Предложение",
        DealStage.Negotiation => "Переговоры",
        DealStage.Won => "Выиграна",
        DealStage.Lost => "Проиграна",
        _ => s.ToString(),
    };

    public static string ToRussian(this ActivityType t) => t switch
    {
        ActivityType.Call => "Звонок",
        ActivityType.Meeting => "Встреча",
        ActivityType.Email => "Письмо",
        ActivityType.Task => "Задача",
        _ => t.ToString(),
    };

    public static string ToRussian(this ActivityStatus s) => s switch
    {
        ActivityStatus.Planned => "Запланирована",
        ActivityStatus.InProgress => "В работе",
        ActivityStatus.Completed => "Завершена",
        ActivityStatus.Cancelled => "Отменена",
        _ => s.ToString(),
    };

    public static string ToRussian(this Priority p) => p switch
    {
        Priority.Low => "Низкий",
        Priority.Normal => "Обычный",
        Priority.High => "Высокий",
        _ => p.ToString(),
    };

    public static string ToRussian(this UserRole r) => r switch
    {
        UserRole.Admin => "Администратор",
        UserRole.Manager => "Менеджер",
        UserRole.Viewer => "Наблюдатель",
        _ => r.ToString(),
    };
}
