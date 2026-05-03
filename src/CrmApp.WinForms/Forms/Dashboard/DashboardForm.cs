using CrmApp.Core.Abstractions;
using CrmApp.Core.Enums;
using CrmApp.WinForms.Localization;
using CrmApp.WinForms.Theming;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace CrmApp.WinForms.Forms.Dashboard;

public partial class DashboardForm : Form
{
    private readonly IDashboardService _service;
    private readonly ILogger<DashboardForm> _logger;

    // Сколько месяцев показываем на линейной диаграмме выручки.
    private const int MonthsOnRevenueChart = 6;

    public DashboardForm(IDashboardService service, ILogger<DashboardForm> logger)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(logger);

        _service = service;
        _logger = logger;

        InitializeComponent();
        // Подкрашиваем легенду pie-chart под текущую тему. Без этого LiveChartsCore
        // рисует текст легенды дефолтным тёмным цветом и в тёмной теме он не виден.
        _statusChart.LegendTextPaint = new SolidColorPaint(ToSk(AppPalette.TextPrimary));
        _statusChart.LegendTextSize = 13;
        Load += async (_, _) => await RefreshAsync();
        // Авто-обновление при возвращении на дашборд: пользователь добавил активность
        // или сделку в другом окне → переключился на дашборд → метрики уже актуальные.
        Activated += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            var summary = await _service.GetSummaryAsync();
            var statuses = await _service.GetCustomersByStatusAsync();
            var monthly = await _service.GetMonthlyRevenueAsync(MonthsOnRevenueChart);

            _customersCard.SetValue(summary.TotalCustomers.ToString());
            _activeDealsCard.SetValue(summary.ActiveDeals.ToString());
            _wonRevenueCard.SetValue(summary.TotalWonRevenue.ToString());
            _forecastCard.SetValue(summary.ForecastRevenue.ToString());
            _overdueCard.SetValue(summary.OverdueActivities.ToString());
            _upcomingCard.SetValue(summary.UpcomingActivities.ToString());

            // Карточка просрочек становится красной, если есть что просрочено.
            _overdueCard.SetAccent(summary.OverdueActivities > 0
                ? AppPalette.Danger
                : AppPalette.Muted);

            UpdateStatusChart(statuses);
            UpdateRevenueChart(monthly);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обновления дашборда");
            MessageBox.Show("Не удалось загрузить данные дашборда: " + ex.Message,
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Pie-серия — по одному PieSeries<int> на статус.
    // Внутри сегмента показываем долю в процентах + абсолютное число клиентов в скобках.
    private void UpdateStatusChart(IReadOnlyDictionary<CustomerStatus, int> statuses)
    {
        // Суммируем только видимые (ненулевые) сегменты — на этой базе считаем проценты.
        var total = statuses.Values.Where(v => v > 0).Sum();
        var series = new List<ISeries>();
        foreach (var status in Enum.GetValues<CustomerStatus>())
        {
            var count = statuses.GetValueOrDefault(status, 0);
            if (count == 0) continue; // не загромождаем диаграмму нулевыми сегментами

            // Процент считаем один раз и фиксируем в замыкании. В RC-версиях LiveCharts2
            // поле p.StackedValue.Share у PieSeries заполняется не всегда — надёжнее иметь свой.
            var percent = total > 0 ? count * 100.0 / total : 0;
            var displayCount = count;

            series.Add(new PieSeries<int>
            {
                Values = [count],
                Name = status.ToRussian(),
                Fill = new SolidColorPaint(ColorForStatus(status)),
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                // Размер метки увеличен с 14 до 22 — чтобы проценты читались с дальнего конца переговорки.
                DataLabelsSize = 22,
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                // Одна строка: SkiaSharp в LiveChartsCore не рендерит \n и показывал missing-glyph квадратик.
                DataLabelsFormatter = _ => $"{percent:F0}% ({displayCount})",
            });
        }

        _statusChart.Series = series;
    }

    private void UpdateRevenueChart(IReadOnlyList<Core.Models.MonthlyRevenuePoint> points)
    {
        var values = points.Select(p => (double)p.Revenue.Amount).ToArray();
        var labels = points.Select(p => MonthLabel(p.Year, p.Month)).ToArray();

        // Цвета подписей и сетки берём из текущей темы — иначе в тёмной теме
        // оси и сетка получаются почти невидимыми.
        var labelPaint = new SolidColorPaint(ToSk(AppPalette.TextPrimary));
        var separatorPaint = new SolidColorPaint(ToSk(AppPalette.Border));

        _revenueChart.Series =
        [
            new ColumnSeries<double>
            {
                Values = values,
                Name = "Выручка",
                Fill = new SolidColorPaint(SKColor.Parse("#2196F3")),
                Stroke = null,
            },
        ];

        _revenueChart.XAxes =
        [
            new Axis
            {
                Labels = labels,
                LabelsRotation = 0,
                TextSize = 12,
                LabelsPaint = labelPaint,
                SeparatorsPaint = separatorPaint,
            },
        ];

        _revenueChart.YAxes =
        [
            new Axis
            {
                Labeler = v => v >= 1_000_000
                    ? $"{v / 1_000_000:0.#} млн"
                    : v >= 1_000
                        ? $"{v / 1_000:0} тыс"
                        : v.ToString("0"),
                TextSize = 12,
                MinLimit = 0,
                LabelsPaint = labelPaint,
                SeparatorsPaint = separatorPaint,
            },
        ];
    }

    // System.Drawing.Color → SkiaSharp.SKColor — нужен для покраски подписей LiveChartsCore.
    private static SKColor ToSk(Color c) => new(c.R, c.G, c.B, c.A);

    private async void OnRefreshClick(object? sender, EventArgs e)
    {
        await RefreshAsync();
    }

    // Цвета сегментов pie-чарта согласованы с семантикой статусов.
    private static SKColor ColorForStatus(CustomerStatus s) => s switch
    {
        CustomerStatus.Lead => SKColor.Parse("#0BC5DD"),       // голубой = новый
        CustomerStatus.Active => SKColor.Parse("#4CAF50"),     // зелёный = активный
        CustomerStatus.Vip => SKColor.Parse("#9C27B0"),        // пурпурный = VIP
        CustomerStatus.Inactive => SKColor.Parse("#9E9E9E"),   // серый = неактивен
        CustomerStatus.Blocked => SKColor.Parse("#DC3545"),    // красный = заблокирован
        _ => SKColor.Parse("#607D8B"),
    };

    private static string MonthLabel(int year, int month)
    {
        var months = new[]
        {
            "янв", "фев", "мар", "апр", "май", "июн",
            "июл", "авг", "сен", "окт", "ноя", "дек",
        };
        return $"{months[month - 1]} {year % 100:00}";
    }
}
