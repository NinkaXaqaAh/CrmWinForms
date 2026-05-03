#nullable enable

using CrmApp.WinForms.Controls;
using CrmApp.WinForms.Theming;
using LiveChartsCore.SkiaSharpView.WinForms;

namespace CrmApp.WinForms.Forms.Dashboard;

partial class DashboardForm
{
    private System.ComponentModel.IContainer? components = null;

    private TableLayoutPanel _root = null!;
    private Panel _toolbar = null!;
    private Label _title = null!;
    private Button _refreshButton = null!;

    private FlowLayoutPanel _cardsPanel = null!;
    private StatCard _customersCard = null!;
    private StatCard _activeDealsCard = null!;
    private StatCard _wonRevenueCard = null!;
    private StatCard _forecastCard = null!;
    private StatCard _overdueCard = null!;
    private StatCard _upcomingCard = null!;

    // Графики LiveChartsCore: левая панель — pie со статусами, правая — line с выручкой.
    private TableLayoutPanel _chartsRow = null!;
    private GroupBox _statusGroup = null!;
    private PieChart _statusChart = null!;
    private GroupBox _revenueGroup = null!;
    private CartesianChart _revenueChart = null!;

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

        _root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(20),
            BackColor = AppPalette.WindowBackground,
        };
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        // Высота ряда карточек подросла под новый StatCard (150px высоты + отступы).
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Тулбар: заголовок слева, кнопка обновления справа.
        // BackColor=Transparent — иначе Panel берёт SystemColors.Control (системный светлый),
        // и AppPalette.TextPrimary на нём в тёмной теме теряет контраст.
        _toolbar = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        _title = new Label
        {
            Text = "Дашборд",
            Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
            ForeColor = AppPalette.TextPrimary,
            AutoSize = true,
            Location = new Point(0, 8),
        };
        _refreshButton = new Button
        {
            Text = "Обновить",
            Size = new Size(120, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppPalette.Surface,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        _refreshButton.FlatAppearance.BorderColor = AppPalette.BorderMuted;
        _refreshButton.Click += OnRefreshClick;

        _toolbar.Controls.Add(_title);
        _toolbar.Controls.Add(_refreshButton);
        _toolbar.Resize += (_, _) =>
            _refreshButton.Location = new Point(_toolbar.Width - _refreshButton.Width - 4, 8);

        // Карточки.
        _cardsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0, 10, 0, 10),
        };

        _customersCard = MakeCard("Клиентов всего", AppPalette.Accent);
        _activeDealsCard = MakeCard("Активные сделки", AppPalette.Warning);
        _wonRevenueCard = MakeCard("Выручка (закрыто)", AppPalette.Success);
        _forecastCard = MakeCard("Прогноз 90 дней", AppPalette.Purple);
        _overdueCard = MakeCard("Просрочено задач", AppPalette.Muted);
        _upcomingCard = MakeCard("Задачи на неделю", AppPalette.Info);

        _cardsPanel.Controls.AddRange(new Control[]
        {
            _customersCard, _activeDealsCard, _wonRevenueCard,
            _forecastCard, _overdueCard, _upcomingCard,
        });

        // Ряд графиков: 2 колонки 50/50.
        _chartsRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
        };
        _chartsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        _chartsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        _chartsRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _statusChart = new PieChart
        {
            Dock = DockStyle.Fill,
            BackColor = AppPalette.Surface,
            LegendPosition = LiveChartsCore.Measure.LegendPosition.Right,
        };
        _statusGroup = new GroupBox
        {
            Text = "Клиенты по статусам",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 10F),
            ForeColor = AppPalette.TextPrimary,
            BackColor = AppPalette.Surface,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 6, 0),
            // FlatStyle.Flat отключает VisualStyles, иначе ForeColor у заголовка
            // GroupBox игнорируется и тёмная тема рисует его системным цветом.
            FlatStyle = FlatStyle.Flat,
        };
        _statusGroup.Controls.Add(_statusChart);

        _revenueChart = new CartesianChart
        {
            Dock = DockStyle.Fill,
            BackColor = AppPalette.Surface,
            LegendPosition = LiveChartsCore.Measure.LegendPosition.Hidden,
        };
        _revenueGroup = new GroupBox
        {
            Text = "Выручка по месяцам",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 10F),
            ForeColor = AppPalette.TextPrimary,
            BackColor = AppPalette.Surface,
            Padding = new Padding(10),
            Margin = new Padding(6, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
        };
        _revenueGroup.Controls.Add(_revenueChart);

        _chartsRow.Controls.Add(_statusGroup, 0, 0);
        _chartsRow.Controls.Add(_revenueGroup, 1, 0);

        _root.Controls.Add(_toolbar, 0, 0);
        _root.Controls.Add(_cardsPanel, 0, 1);
        _root.Controls.Add(_chartsRow, 0, 2);

        Controls.Add(_root);

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1100, 700);
        WindowState = FormWindowState.Maximized;
        Text = "Дашборд";
        Font = new Font("Segoe UI", 9F);
        BackColor = AppPalette.WindowBackground;

        ResumeLayout(false);
    }

    private static StatCard MakeCard(string title, Color accent)
    {
        var card = new StatCard
        {
            Title = title,
            Margin = new Padding(0, 0, 12, 0),
        };
        card.SetAccent(accent);
        return card;
    }
}
