using CrmApp.Core.Abstractions;
using CrmApp.Infrastructure;
using CrmApp.Infrastructure.Services;
using CrmApp.WinForms.Composition;
using CrmApp.WinForms.Forms.Auth;
using CrmApp.WinForms.Forms.Common;
using CrmApp.WinForms.Theming;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CrmApp.WinForms;

// Точка входа. Поднимаем DI-контейнер, читаем конфиг, прогоняем сидер,
// показываем LoginForm; при успехе - запускаем MDI-родитель MainForm.
internal static class Program
{
    [STAThread]
    private static int Main()
    {
        // Стандартная инициализация WinForms на .NET 6+.
        // Источник true-type-шрифтов и DPI-режим уже заданы в csproj через ApplicationXxx-свойства.
        ApplicationConfiguration.Initialize();

        using var services = BuildServices();

        // Применяем сохранённую пользователем тему ДО создания любых форм —
        // тогда конструкторы Designer'ов читают уже актуальную палитру.
        var themeStore = services.GetRequiredService<ThemeStore>();
        AppPalette.Apply(themeStore.Load());

        // Сидим демо-данные синхронно ДО показа любых форм - чтобы LoginForm видела пользователей.
        // GetAwaiter().GetResult() здесь оправдан: мы ещё не в UI-цикле, deadlock'а не будет.
        try
        {
            var seeder = services.GetRequiredService<SeedDataService>();
            seeder.EnsureSeededAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            ShowFatal("Не удалось инициализировать данные приложения", ex);
            return 1;
        }

        // Цикл: логин → MainForm → если пользователь нажал "Сменить" — снова логин.
        // Сценарии выхода из цикла:
        //   - login был отменён  → выходим;
        //   - MainForm закрыта при IsAuthenticated == true (обычное закрытие окна) → выходим;
        //   - MainForm закрыта при IsAuthenticated == false (logout) → возвращаемся к логину.
        var userContext = services.GetRequiredService<ICurrentUserContext>();

        while (true)
        {
            using (var login = services.GetRequiredService<LoginForm>())
            {
                if (login.ShowDialog() != DialogResult.OK)
                {
                    return 0;
                }
            }

            try
            {
                var main = services.GetRequiredService<MainForm>();
                Application.Run(main);
            }
            catch (Exception ex)
            {
                ShowFatal("Критическая ошибка приложения", ex);
                return 2;
            }

            if (userContext.IsAuthenticated)
            {
                return 0;
            }
        }
    }

    // Строим DI-контейнер: конфиг + Infrastructure (репозитории, сервисы) + Presentation (формы).
    private static ServiceProvider BuildServices()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables(prefix: "CRM_")
            .Build();

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);

        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddDebug();
            // Console-логгер виден только в Debug, потому что OutputType=WinExe
            // не имеет stdout. В Debug под VS он пишется в Output/Debug.
            builder.AddConsole();
        });

        services.AddInfrastructure(configuration);
        services.AddPresentation();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }

    // Показываем критическую ошибку пользователю - до или после падения UI.
    private static void ShowFatal(string title, Exception ex)
    {
        MessageBox.Show(
            $"{title}\n\n{ex.Message}\n\n{ex.GetType().Name}",
            "Ошибка",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
