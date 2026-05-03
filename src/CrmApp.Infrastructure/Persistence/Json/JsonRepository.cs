using System.Text.Json;
using System.Text.Json.Serialization;
using CrmApp.Core.Abstractions;
using CrmApp.Core.Common;
using CrmApp.Core.Exceptions;
using CrmApp.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CrmApp.Infrastructure.Persistence.Json;

// Базовый generic-репозиторий: один JSON-файл на сущность.
// Все операции потокобезопасны (SemaphoreSlim, async).
// Запись идёт через временный файл + atomic rename, чтобы файл не повредился при сбое.
// Перед перезаписью оригинал переносится в .bak (опционально).
// IDisposable — потому что владеем SemaphoreSlim. DI-контейнер высвобождает Singleton'ы
// при остановке приложения.
public abstract class JsonRepository<T> : IRepository<T>, IDisposable where T : class, IEntity, IAuditable
{
    private readonly JsonStorageOptions _options;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;

    private List<T>? _cache;

    protected JsonRepository(
        string fileName,
        IOptions<JsonStorageOptions> options,
        IDateTimeProvider clock,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _clock = clock;
        _logger = logger;

        var dataFolder = ExpandDataFolder(_options.DataFolder);
        Directory.CreateDirectory(dataFolder);
        _filePath = Path.Combine(dataFolder, fileName);

        _jsonOptions = BuildJsonOptions(_options.WriteIndented);
    }

    // Развёртывает плейсхолдеры вида %APPDATA%, %LOCALAPPDATA% в реальные пути.
    // Environment.ExpandEnvironmentVariables на Windows справляется case-insensitive — отдельная
    // нормализация регистра не нужна.
    private static string ExpandDataFolder(string folder)
    {
        return Environment.ExpandEnvironmentVariables(folder);
    }

    private static JsonSerializerOptions BuildJsonOptions(bool indented)
    {
        var opts = new JsonSerializerOptions
        {
            WriteIndented = indented,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        opts.Converters.Add(new MoneyJsonConverter());
        opts.Converters.Add(new DateOnlyJsonConverter());
        opts.Converters.Add(new JsonStringEnumConverter());
        return opts;
    }

    // Загрузка кэша при первом обращении. Кэш потокобезопасен через _gate.
    private async Task<List<T>> LoadAsync(CancellationToken ct)
    {
        if (_cache is not null) return _cache;

        if (!File.Exists(_filePath))
        {
            _cache = new List<T>();
            return _cache;
        }

        try
        {
            await using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var loaded = await JsonSerializer.DeserializeAsync<List<T>>(fs, _jsonOptions, ct).ConfigureAwait(false);
            _cache = loaded ?? new List<T>();
            _logger.LogInformation("Загружено {Count} записей из {File}", _cache.Count, _filePath);
            return _cache;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Файл {File} повреждён - инициализирую пустой набор", _filePath);
            _cache = new List<T>();
            return _cache;
        }
    }

    // Atomic-запись: пишем в .tmp, потом переименовываем поверх оригинала.
    // Если включены бэкапы - старый файл переименовывается в .bak перед заменой.
    private async Task SaveAsync(List<T> items, CancellationToken ct)
    {
        var tmpPath = _filePath + ".tmp";
        var bakPath = _filePath + ".bak";

        await using (var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(fs, items, _jsonOptions, ct).ConfigureAwait(false);
        }

        if (File.Exists(_filePath))
        {
            if (_options.CreateBackups)
            {
                if (File.Exists(bakPath)) File.Delete(bakPath);
                File.Move(_filePath, bakPath);
            }
            else
            {
                File.Delete(_filePath);
            }
        }

        File.Move(tmpPath, _filePath);
        _logger.LogDebug("Сохранено {Count} записей в {File}", items.Count, _filePath);
    }

    public async Task<T> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await FindByIdAsync(id, ct).ConfigureAwait(false);
        if (item is null)
        {
            throw new EntityNotFoundException(typeof(T).Name, id);
        }
        return item;
    }

    public async Task<T?> FindByIdAsync(Guid id, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var items = await LoadAsync(ct).ConfigureAwait(false);
            return items.FirstOrDefault(x => x.Id == id);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var items = await LoadAsync(ct).ConfigureAwait(false);
            return items.ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AddAsync(T entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var items = await LoadAsync(ct).ConfigureAwait(false);
            if (items.Any(x => x.Id == entity.Id))
            {
                throw new InvalidOperationException(
                    $"Сущность {typeof(T).Name} с Id={entity.Id} уже существует");
            }

            var now = _clock.Now;
            entity.CreatedAt = now;
            entity.UpdatedAt = now;

            items.Add(entity);
            await SaveAsync(items, ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var items = await LoadAsync(ct).ConfigureAwait(false);
            var index = items.FindIndex(x => x.Id == entity.Id);
            if (index < 0)
            {
                throw new EntityNotFoundException(typeof(T).Name, entity.Id);
            }

            // Сохраняем CreatedAt оригинала, обновляем UpdatedAt.
            entity.CreatedAt = items[index].CreatedAt;
            entity.UpdatedAt = _clock.Now;

            items[index] = entity;
            await SaveAsync(items, ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var items = await LoadAsync(ct).ConfigureAwait(false);
            var removed = items.RemoveAll(x => x.Id == id);
            if (removed == 0)
            {
                throw new EntityNotFoundException(typeof(T).Name, id);
            }
            await SaveAsync(items, ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        return await FindByIdAsync(id, ct).ConfigureAwait(false) is not null;
    }

    // Защищённый метод для специализированных репозиториев.
    // Они получают копию списка для своих запросов, не нарушая инкапсуляции.
    protected async Task<List<T>> SnapshotAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var items = await LoadAsync(ct).ConfigureAwait(false);
            return items.ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gate.Dispose();
        }
    }
}
