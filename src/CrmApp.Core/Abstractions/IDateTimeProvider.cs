namespace CrmApp.Core.Abstractions;

// Абстракция системных часов.
// В тестах подменяется FakeClock'ом — без неё привязки к DateTime.Now мешали бы юнит-тестам.
public interface IDateTimeProvider
{
    DateTime Now { get; }
    DateOnly Today { get; }
}
