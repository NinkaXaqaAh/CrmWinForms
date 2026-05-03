using CrmApp.WinForms.Theming;

namespace CrmApp.WinForms.Controls;

// Карточка-метрика для дашборда.
// Заголовок (мелкий, серый) и значение (очень крупное, чёрное) — чтобы цифра
// сразу бросалась в глаза при беглом взгляде на дашборд.
// Левая цветная полоска — акцент (зелёный/красный/серый) для визуального сигнала.
public sealed class StatCard : UserControl
{
    // Базовый размер шрифта значения. Увеличен с 18F до 38F (≈210%) по запросу:
    // цифры должны "сразу кидаться в глаза директору".
    private const float ValueFontSizeMax = 38F;
    // Минимальный шрифт после автомасштаба — чтобы длинные суммы вроде
    // "1 122 000,00 ₽" не превращались в "1 122 0...".
    private const float ValueFontSizeMin = 18F;

    private readonly Label _titleLabel;
    private readonly Label _valueLabel;
    private readonly Panel _accentBar;

    public StatCard()
    {
        SuspendLayout();

        BackColor = AppPalette.Surface;
        Padding = new Padding(0);
        // Карточка стала шире и выше, чтобы вместить крупный шрифт.
        Size = new Size(280, 150);

        _accentBar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 4,
            BackColor = AppPalette.Accent,
        };

        _titleLabel = new Label
        {
            Location = new Point(20, 18),
            AutoSize = true,
            Font = new Font("Segoe UI", 10F),
            ForeColor = AppPalette.Muted,
            Text = "Заголовок",
        };

        _valueLabel = new Label
        {
            Location = new Point(20, 50),
            Size = new Size(248, 90),
            Font = new Font("Segoe UI Semibold", ValueFontSizeMax, FontStyle.Bold),
            ForeColor = AppPalette.TextPrimary,
            Text = "0",
            // AutoEllipsis отключён — мы вместо обрезки с ... уменьшаем шрифт под длину текста.
            AutoEllipsis = false,
            // TextAlign — слева сверху, чтобы цифра не "плавала" между размерами.
            TextAlign = ContentAlignment.MiddleLeft,
        };

        Controls.Add(_valueLabel);
        Controls.Add(_titleLabel);
        Controls.Add(_accentBar);

        ResumeLayout(false);
    }

    public string Title
    {
        get => _titleLabel.Text;
        set => _titleLabel.Text = value;
    }

    public void SetValue(string value)
    {
        _valueLabel.Text = value;
        _valueLabel.Font = FitFont(value, _valueLabel.Width, _valueLabel.Height);
    }

    public void SetAccent(Color color)
    {
        _accentBar.BackColor = color;
    }

    // Подбираем размер шрифта так, чтобы текст value целиком вмещался в области label.
    // Идём от максимального размера вниз с шагом 2pt — это быстро (max 10 итераций) и достаточно точно.
    private static Font FitFont(string text, int maxWidth, int maxHeight)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new Font("Segoe UI Semibold", ValueFontSizeMax, FontStyle.Bold);
        }

        for (var size = ValueFontSizeMax; size >= ValueFontSizeMin; size -= 2F)
        {
            var font = new Font("Segoe UI Semibold", size, FontStyle.Bold);
            var measured = TextRenderer.MeasureText(text, font);
            if (measured.Width <= maxWidth && measured.Height <= maxHeight)
            {
                return font;
            }
            font.Dispose();
        }

        // Если даже минимальный не влезает — отдадим минимальный, текст всё равно будет читаемым.
        return new Font("Segoe UI Semibold", ValueFontSizeMin, FontStyle.Bold);
    }

    // Тень снизу + 1px-граница — имитация material-карточки.
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(AppPalette.Border, 1);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}
