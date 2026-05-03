using CrmApp.Core.Abstractions;
using CrmApp.Core.Validation;
using CrmApp.WinForms.Forms.Activities;
using CrmApp.WinForms.Forms.Auth;
using CrmApp.WinForms.Forms.Common;
using CrmApp.WinForms.Forms.Customers;
using CrmApp.WinForms.Forms.Dashboard;
using CrmApp.WinForms.Forms.Deals;
using CrmApp.WinForms.Forms.Products;
using CrmApp.WinForms.Forms.Search;
using CrmApp.WinForms.Forms.Users;
using CrmApp.WinForms.Search;
using CrmApp.WinForms.Theming;
using Microsoft.Extensions.DependencyInjection;

namespace CrmApp.WinForms.Composition;

// Регистрация форм, валидаторов и презентационных сервисов в DI-контейнере.
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        // Текущий пользователь - один на приложение.
        services.AddSingleton<ICurrentUserContext, CurrentUserContext>();

        // Хранилище выбранной темы — Singleton, файл один на пользователя.
        services.AddSingleton<ThemeStore>();

        // Глобальный поиск — Singleton, репозитории внутри уже потокобезопасные.
        services.AddSingleton<ISearchService, SearchService>();

        // Валидаторы - stateless, можно Singleton.
        services.AddSingleton<CustomerValidator>();
        services.AddSingleton<DealValidator>();
        services.AddSingleton<ActivityValidator>();
        services.AddSingleton<UserValidator>();
        services.AddSingleton<ProductValidator>();

        // Формы аутентификации
        services.AddTransient<LoginForm>();

        // Главное окно - Transient. Был Singleton, но в этом случае повторное
        // получение MainForm (после "Сменить пользователя") возвращало бы уже
        // закрытый и dispose'нутый экземпляр. Один MDI-родитель в каждый момент
        // обеспечивается циклом в Program.Main, а не Singleton-регистрацией.
        services.AddTransient<MainForm>();

        // Дочерние MDI-формы - Transient (новый экземпляр на каждый Open).
        services.AddTransient<DashboardForm>();
        services.AddTransient<CustomerListForm>();
        services.AddTransient<CustomerEditForm>();
        services.AddTransient<DealListForm>();
        services.AddTransient<DealEditForm>();
        services.AddTransient<ActivityListForm>();
        services.AddTransient<ActivityEditForm>();
        services.AddTransient<ProductListForm>();
        services.AddTransient<ProductEditForm>();
        services.AddTransient<UserListForm>();
        services.AddTransient<UserEditForm>();
        services.AddTransient<SearchDialog>();

        return services;
    }
}
