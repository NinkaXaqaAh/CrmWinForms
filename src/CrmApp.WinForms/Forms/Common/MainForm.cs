using CrmApp.Core.Abstractions;
using CrmApp.Core.Enums;
using CrmApp.WinForms.Forms.Activities;
using CrmApp.WinForms.Forms.Customers;
using CrmApp.WinForms.Forms.Dashboard;
using CrmApp.WinForms.Forms.Deals;
using CrmApp.WinForms.Forms.Products;
using CrmApp.WinForms.Forms.Users;
using CrmApp.WinForms.Theming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CrmApp.WinForms.Forms.Common;

public partial class MainForm : Form
{
    private readonly IServiceProvider _services;
    private readonly ICurrentUserContext _userContext;
    private readonly ThemeStore _themeStore;
    private readonly ILogger<MainForm> _logger;

    public MainForm(
        IServiceProvider services,
        ICurrentUserContext userContext,
        ThemeStore themeStore,
        ILogger<MainForm> logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(userContext);
        ArgumentNullException.ThrowIfNull(themeStore);
        ArgumentNullException.ThrowIfNull(logger);

        _services = services;
        _userContext = userContext;
        _themeStore = themeStore;
        _logger = logger;

        InitializeComponent();

        // Иконка приложения в заголовке окна и панели задач.
        Icon = AppIcon.Default;

        // Заголовок отражает вошедшего пользователя.
        var u = _userContext.Current;
        Text = u is null
            ? "CRM для малого бизнеса"
            : $"CRM для малого бизнеса — {u.FullName} ({u.Role})";

        _userStatusLabel.Text = u is null
            ? "Не авторизован"
            : $"{u.FullName} | {u.Role}";

        // Управление пользователями — только для Admin'а. Скрываем пункт меню для всех остальных.
        var isAdmin = u?.Role == UserRole.Admin;
        _menuUsers.Visible = isAdmin;
        _menuUsersSeparator.Visible = isAdmin;

        // Подсвечиваем галкой текущую тему в меню "Вид → Тема".
        _menuThemeLight.Checked = AppPalette.CurrentTheme == AppTheme.Light;
        _menuThemeDark.Checked = AppPalette.CurrentTheme == AppTheme.Dark;

        Load += OnLoadedShowDashboard;
    }

    private void OnLoadedShowDashboard(object? sender, EventArgs e)
    {
        OpenChild<DashboardForm>();
    }

    // Универсальный открыватель MDI-форм.
    // Если форма того же типа уже открыта - активируем её, иначе создаём новую.
    private void OpenChild<TForm>() where TForm : Form
    {
        var existing = MdiChildren.OfType<TForm>().FirstOrDefault();
        if (existing is not null)
        {
            existing.Activate();
            return;
        }

        var form = _services.GetRequiredService<TForm>();
        form.MdiParent = this;
        form.WindowState = FormWindowState.Maximized;
        // FormClosed срабатывает ДО того, как WinForms удаляет форму из MdiChildren
        // (это известная особенность). Поэтому если просто звать UpdateMenuChecks тут,
        // он находит "уже закрытую" форму и оставляет галку. BeginInvoke откладывает
        // пересчёт на следующую итерацию message-loop'а — к тому моменту коллекция актуальна.
        form.FormClosed += (_, _) => BeginInvoke(new Action(UpdateMenuChecks));
        form.Show();
        UpdateMenuChecks();

        _logger.LogDebug("Открыта MDI-форма {Form}", typeof(TForm).Name);
    }

    private void UpdateMenuChecks()
    {
        // Подсветка пунктов меню для уже открытых форм - типичный UX MDI-приложений.
        _menuDashboard.Checked = MdiChildren.OfType<DashboardForm>().Any();
        _menuCustomers.Checked = MdiChildren.OfType<CustomerListForm>().Any();
        _menuDeals.Checked = MdiChildren.OfType<DealListForm>().Any();
        _menuActivities.Checked = MdiChildren.OfType<ActivityListForm>().Any();
        _menuProducts.Checked = MdiChildren.OfType<ProductListForm>().Any();
    }

    private void OnDashboardClick(object? sender, EventArgs e) => OpenChild<DashboardForm>();
    private void OnCustomersClick(object? sender, EventArgs e) => OpenChild<CustomerListForm>();
    private void OnDealsClick(object? sender, EventArgs e) => OpenChild<DealListForm>();
    private void OnActivitiesClick(object? sender, EventArgs e) => OpenChild<ActivityListForm>();
    private void OnProductsClick(object? sender, EventArgs e) => OpenChild<ProductListForm>();
    private void OnUsersClick(object? sender, EventArgs e) => OpenChild<UserListForm>();

    // Сменить пользователя: чистим контекст и закрываем главное окно.
    // Program.Main увидит, что IsAuthenticated == false, и снова покажет LoginForm.
    private void OnLogoutClick(object? sender, EventArgs e)
    {
        var ok = MessageBox.Show(
            "Сменить пользователя? Все открытые окна будут закрыты.",
            "Смена пользователя",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (ok == DialogResult.Yes)
        {
            _userContext.Clear();
            Close();
        }
    }

    // Полный выход — просто закрываем главное окно с сохранённым контекстом.
    // Program.Main увидит IsAuthenticated == true и завершит процесс.
    private void OnExitClick(object? sender, EventArgs e) => Close();

    private void OnAboutClick(object? sender, EventArgs e)
    {
        MessageBox.Show(
            "CRM для малого бизнеса\n\n" +
            "Учебный проект по итогам лекции 6.1.\n" +
            "Архитектура: Clean Architecture, .NET 10, WinForms (MDI).\n" +
            "Хранение: JSON-файлы.",
            "О программе",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void OnTileHorizontalClick(object? sender, EventArgs e) => LayoutMdi(MdiLayout.TileHorizontal);
    private void OnTileVerticalClick(object? sender, EventArgs e) => LayoutMdi(MdiLayout.TileVertical);
    private void OnCascadeClick(object? sender, EventArgs e) => LayoutMdi(MdiLayout.Cascade);

    private void OnThemeLightClick(object? sender, EventArgs e) => SwitchTheme(AppTheme.Light);
    private void OnThemeDarkClick(object? sender, EventArgs e) => SwitchTheme(AppTheme.Dark);

    // Сохраняем выбор + просим перезапустить. Live-применение требует обхода
    // дерева контролов всех открытых форм, что в WinForms делается негарантированно
    // и приводит к артефактам перерисовки. Перезапуск — простая и предсказуемая модель.
    private void SwitchTheme(AppTheme theme)
    {
        if (AppPalette.CurrentTheme == theme) return;

        try
        {
            _themeStore.Save(theme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось сохранить выбор темы");
            MessageBox.Show("Не удалось сохранить настройку темы: " + ex.Message,
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var name = theme == AppTheme.Dark ? "тёмная" : "светлая";
        MessageBox.Show(
            $"Тема сохранена ({name}). Перезапустите приложение для применения.",
            "Тема",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}
