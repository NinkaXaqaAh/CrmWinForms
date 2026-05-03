using System.Text.Json;
using System.Text.Json.Serialization;
using CrmApp.Core.Common;

namespace CrmApp.Infrastructure.Persistence.Json;

// JSON-конвертер для value object Money.
// Без него System.Text.Json пытается сериализовать readonly record struct
// и записывает в файл как { "Amount": 100, "Currency": "RUB" } - но это работает,
// если конвертер не нужен. Однако при десериализации с record struct и init-only
// свойствами бывают тонкости с порядком полей и null-currency, поэтому пишем явно.
public sealed class MoneyJsonConverter : JsonConverter<Money>
{
    public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Ожидался объект для Money");
        }

        decimal amount = 0m;
        string currency = "RUB";

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return new Money(amount, currency);
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "Amount" or "amount":
                    amount = reader.GetDecimal();
                    break;
                case "Currency" or "currency":
                    currency = reader.GetString() ?? "RUB";
                    break;
            }
        }

        throw new JsonException("Не закрыт объект Money");
    }

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("Amount", value.Amount);
        writer.WriteString("Currency", value.Currency);
        writer.WriteEndObject();
    }
}
