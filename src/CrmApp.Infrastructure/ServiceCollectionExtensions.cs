using CrmApp.Core.Abstractions;
using CrmApp.Core.Common;
using CrmApp.Infrastructure.Configuration;
using CrmApp.Infrastructure.Persistence.Json;
using CrmApp.Infrastructure.Reporting;
using CrmApp.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CrmApp.Infrastructure;

// Точка регистрации инфраструктурного слоя в DI.
// Один метод AddInfrastructure() в Program.cs подключает всё:
// настройки, репозитории, сервисы экспорта, сидер.
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Настройки JSON-хранилища
        services.Configure<JsonStorageOptions>(
            configuration.GetSection(JsonStorageOptions.SectionName));

        // Системные часы (можно будет подменить в тестах)
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // Репозитории - Singleton, потому что у каждого внутри кэш и SemaphoreSlim.
        services.AddSingleton<ICustomerRepository, JsonCustomerRepository>();
        services.AddSingleton<IDealRepository, JsonDealRepository>();
        services.AddSingleton<IActivityRepository, JsonActivityRepository>();
        services.AddSingleton<IUserRepository, JsonUserRepository>();
        services.AddSingleton<IProductRepository, JsonProductRepository>();

        // Доменные сервисы
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IDealPipelineService, DealPipelineService>();
        services.AddSingleton<IDashboardService, DashboardService>();

        // Сидер - Transient, потому что используется один раз при старте.
        services.AddTransient<SeedDataService>();

        // Экспорт отчётов — Singleton, состояния нет, потокобезопасны.
        services.AddSingleton<IExcelExportService, ExcelExportService>();
        services.AddSingleton<IPdfReportService, PdfReportService>();

        return services;
    }
}
