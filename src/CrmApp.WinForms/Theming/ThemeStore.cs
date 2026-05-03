using System.Text.Json;

namespace CrmApp.WinForms.Theming;

// Простое JSON-хранилище выбранной пользователем темы.
// Файл лежит в %APPDATA%\CrmApp\theme.json — рядом с данными приложения.
// Не используем Microsoft.Extensions.Configuration Writable, чтобы не утаскивать
// дополнительные зависимости и оставить операцию простой и атомарной.
public sealed class ThemeStore
{
    private readonly string _filePath;

    public ThemeStore()
    {
        var folder = Environment.ExpandEnvironmentVariables("%APPDATA%\\CrmApp");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "theme.json");
    }

    public AppTheme Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return AppTheme.Light;
            var text = File.ReadAllText(_filePath);
            var dto = JsonSerializer.Deserialize<ThemeDto>(text);
            return dto?.Theme ?? AppTheme.Light;
        }
        catch
        {
            // Любая ошибка чтения/парсинга — откатываемся к светлой теме как безопасному дефолту.
            return AppTheme.Light;
        }
    }

    public void Save(AppTheme theme)
    {
        var json = JsonSerializer.Serialize(new ThemeDto { Theme = theme });
        File.WriteAllText(_filePath, json);
    }

    private sealed class ThemeDto
    {
        public AppTheme Theme { get; set; }
    }
}
