namespace CrmApp.Infrastructure.Configuration;

// Настройки JSON-хранилища, биндятся из секции "JsonStorage" в appsettings.json.
public sealed class JsonStorageOptions
{
    public const string SectionName = "JsonStorage";

    // Папка для JSON-файлов. Поддерживает плейсхолдеры окружения, например "%APPDATA%\\CrmApp".
    public string DataFolder { get; set; } = "%APPDATA%\\CrmApp";

    // Писать с отступами (для удобства чтения в редакторе).
    public bool WriteIndented { get; set; } = true;

    // Перед перезаписью переименовывать старый файл в .bak.
    public bool CreateBackups { get; set; } = true;
}
