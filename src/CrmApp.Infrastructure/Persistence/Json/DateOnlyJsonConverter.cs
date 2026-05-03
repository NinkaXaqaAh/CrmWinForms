using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CrmApp.Infrastructure.Persistence.Json;

// System.Text.Json в .NET 8+ умеет DateOnly из коробки, но формат по умолчанию
// зависит от культуры. Фиксируем строгий ISO 8601 формат (yyyy-MM-dd) -
// чтобы файлы хранилища не сломались, если пользователь сменит региональные настройки.
public sealed class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string Format = "yyyy-MM-dd";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        if (string.IsNullOrEmpty(s))
        {
            throw new JsonException("Пустая строка для DateOnly");
        }

        return DateOnly.ParseExact(s, Format, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }
}
