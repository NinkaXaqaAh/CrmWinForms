namespace CrmApp.Core.Exceptions;

// Исключение нарушения доменного правила.
// Кидается доменными сервисами (например, при недопустимом переходе по этапам сделки).
// UI ловит DomainException и показывает текст пользователю как валидационную ошибку,
// а не как технический сбой.
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}
