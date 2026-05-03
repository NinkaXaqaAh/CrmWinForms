using CrmApp.Core.Abstractions;

namespace CrmApp.Core.Common;

// Реальный поставщик времени поверх DateTime.Now / DateTime.Today.
// Используется в DI как Singleton; в тестах подменяется фейком.
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime Now => DateTime.Now;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
}
