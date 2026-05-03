namespace CrmApp.Core.Enums;

// Тип взаимодействия с клиентом.
public enum ActivityType
{
    Call = 0,       // Телефонный звонок.
    Meeting = 1,    // Встреча.
    Email = 2,      // Письмо.
    Task = 3,       // Внутренняя задача без прямого контакта.
}
