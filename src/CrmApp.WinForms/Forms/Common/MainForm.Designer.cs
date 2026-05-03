#nullable enable

using CrmApp.WinForms.Theming;

namespace CrmApp.WinForms.Forms.Common;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;

    private MenuStrip _menuStrip = null!;
    private ToolStripMenuItem _menuFile = null!;
    private ToolStripMenuItem _menuView = null!;
    private ToolStripMenuItem _menuWindow = null!;
    private ToolStripMenuItem _menuHelp = null!;

    private ToolStripMenuItem _menuDashboard = null!;
    private ToolStripMenuItem _menuCustomers = null!;
    private ToolStripMenuItem _menuDeals = null!;
    private ToolStripMenuItem _menuActivities = null!;
    private ToolStripMenuItem _menuProducts = null!;

    private ToolStripMenuItem _menuUsers = null!;
    private ToolStripSeparator _menuUsersSeparator = null!;
    private ToolStripMenuItem _menuLogout = null!;
    private ToolStripMenuItem _menuExit = null!;
    private ToolStripMenuItem _menuAbout = null!;

    private ToolStripMenuItem _menuTileH = null!;
    private ToolStripMenuItem _menuTileV = null!;
    private ToolStripMenuItem _menuCascade = null!;

    private ToolStripMenuItem _menuThemeRoot = null!;
    private ToolStripMenuItem _menuThemeLight = null!;
    private ToolStripMenuItem _menuThemeDark = null!;

    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _userStatusLabel = null!;
    private ToolStripStatusLabel _spacerLabel = null!;
    private ToolStripStatusLabel _versionLabel = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        // -- Меню --
        _menuStrip = new MenuStrip
        {
            Padding = new Padding(6, 2, 0, 2),
            Font = new Font("Segoe UI", 9.5F),
        };

        _menuFile = new ToolStripMenuItem("&Файл");
        // Пункт "Управление пользователями" виден только Admin'у — флаг Visible выставляется в MainForm.cs.
        _menuUsers = new ToolStripMenuItem("Управление пользователями…", null, OnUsersClick);
        _menuUsersSeparator = new ToolStripSeparator();
        _menuLogout = new ToolStripMenuItem("Сменить пользователя", null, OnLogoutClick);
        _menuExit = new ToolStripMenuItem("Выход", null, OnExitClick) { ShortcutKeys = Keys.Alt | Keys.F4 };
        _menuFile.DropDownItems.AddRange(new ToolStripItem[]
        {
            _menuUsers, _menuUsersSeparator, _menuLogout, new ToolStripSeparator(), _menuExit
        });

        _menuView = new ToolStripMenuItem("&Вид");
        _menuDashboard = new ToolStripMenuItem("Дашборд", null, OnDashboardClick) { ShortcutKeys = Keys.Control | Keys.D1 };
        _menuCustomers = new ToolStripMenuItem("Клиенты", null, OnCustomersClick) { ShortcutKeys = Keys.Control | Keys.D2 };
        _menuDeals = new ToolStripMenuItem("Сделки", null, OnDealsClick) { ShortcutKeys = Keys.Control | Keys.D3 };
        _menuActivities = new ToolStripMenuItem("Активности", null, OnActivitiesClick) { ShortcutKeys = Keys.Control | Keys.D4 };
        _menuProducts = new ToolStripMenuItem("Товары", null, OnProductsClick) { ShortcutKeys = Keys.Control | Keys.D5 };

        // Подменю выбора темы. Галка ставится в MainForm.cs по AppPalette.CurrentTheme.
        _menuThemeRoot = new ToolStripMenuItem("Тема");
        _menuThemeLight = new ToolStripMenuItem("Светлая", null, OnThemeLightClick);
        _menuThemeDark = new ToolStripMenuItem("Тёмная", null, OnThemeDarkClick);
        _menuThemeRoot.DropDownItems.AddRange(new ToolStripItem[]
        {
            _menuThemeLight, _menuThemeDark,
        });

        _menuView.DropDownItems.AddRange(new ToolStripItem[]
        {
            _menuDashboard, _menuCustomers, _menuDeals, _menuActivities, _menuProducts,
            new ToolStripSeparator(),
            _menuThemeRoot,
        });

        _menuWindow = new ToolStripMenuItem("&Окна");
        _menuTileH = new ToolStripMenuItem("Горизонтально", null, OnTileHorizontalClick);
        _menuTileV = new ToolStripMenuItem("Вертикально", null, OnTileVerticalClick);
        _menuCascade = new ToolStripMenuItem("Каскадом", null, OnCascadeClick);
        _menuWindow.DropDownItems.AddRange(new ToolStripItem[]
        {
            _menuTileH, _menuTileV, _menuCascade
        });

        _menuHelp = new ToolStripMenuItem("&Справка");
        _menuAbout = new ToolStripMenuItem("О программе", null, OnAboutClick);
        _menuHelp.DropDownItems.Add(_menuAbout);

        _menuStrip.Items.AddRange(new ToolStripItem[]
        {
            _menuFile, _menuView, _menuWindow, _menuHelp
        });

        // MDI-меню "Окна" автоматически наполняется списком открытых дочерних форм.
        _menuStrip.MdiWindowListItem = _menuWindow;

        // -- Статусбар --
        // BackColor + ForeColor обязаны быть из AppPalette, иначе ToolStripProfessionalRenderer
        // нарисует чёрный текст на тёмной теме.
        _statusStrip = new StatusStrip
        {
            Padding = new Padding(6, 0, 6, 0),
            BackColor = AppPalette.SurfaceMuted,
            ForeColor = AppPalette.TextPrimary,
        };
        _userStatusLabel = new ToolStripStatusLabel("Не авторизован")
        {
            Spring = false,
            BorderSides = ToolStripStatusLabelBorderSides.Right,
            BorderStyle = Border3DStyle.Etched,
            ForeColor = AppPalette.TextPrimary,
        };
        _spacerLabel = new ToolStripStatusLabel { Spring = true };
        _versionLabel = new ToolStripStatusLabel("v1.0.0")
        {
            ForeColor = AppPalette.TextPrimary,
        };
        _statusStrip.Items.AddRange(new ToolStripItem[]
        {
            _userStatusLabel, _spacerLabel, _versionLabel
        });

        // -- Форма --
        Controls.Add(_menuStrip);
        Controls.Add(_statusStrip);
        MainMenuStrip = _menuStrip;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1280, 800);
        IsMdiContainer = true;
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        Text = "CRM для малого бизнеса";
        Font = new Font("Segoe UI", 9F);
        BackColor = AppPalette.WindowBackground;

        ResumeLayout(false);
        PerformLayout();
    }
}
