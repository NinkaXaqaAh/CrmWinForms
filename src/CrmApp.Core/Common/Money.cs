using System.Globalization;

namespace CrmApp.Core.Common;

// Value-object для денежной суммы. Record struct — потому что:
// - immutable по смыслу (deal.Amount = new Money(...) присваивает новый объект целиком),
// - бесплатное равенство по значению,
// - копируется по value, не нужен Clone.
public readonly record struct Money(decimal Amount, string Currency)
{
    // Конструктор по умолчанию рублёвый — чтобы new Money(100m) был валидным.
    public Money(decimal amount) : this(amount, DefaultCurrency) { }

    public const string DefaultCurrency = "RUB";

    public static Money Zero(string currency = DefaultCurrency) => new(0m, currency);

    public Money Add(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Нельзя складывать суммы в разных валютах: {Currency} и {other.Currency}");
        }
        return new Money(Amount + other.Amount, Currency);
    }

    public override string ToString()
    {
        // ru-RU группировка тысяч, две знака после запятой, символ валюты после числа.
        var ru = CultureInfo.GetCultureInfo("ru-RU");
        var symbol = Currency switch
        {
            "RUB" => "₽",
            "USD" => "$",
            "EUR" => "€",
            _ => Currency,
        };
        return $"{Amount.ToString("N2", ru)} {symbol}";
    }
}
