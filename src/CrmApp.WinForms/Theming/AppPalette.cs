namespace CrmApp.WinForms.Theming;

// Цветовая палитра приложения. Mutable static — на старте Program.Main
// в неё применяется выбранная пользователем тема (см. ThemeStore).
//
// Решение по архитектуре: тема применяется только до создания форм и требует
// перезапуска для смены. Это сознательный компромисс — иначе пришлось бы вручную
// обходить дерево контролов всех открытых MDI-окон, что в WinForms сложно сделать
// корректно без багов перерисовки.
public static class AppPalette
{
    private static AppTheme _theme = AppTheme.Light;

    public static AppTheme CurrentTheme => _theme;

    // Бренд / семантические цвета (в обеих темах одинаковые).
    public static Color Accent { get; private set; }
    public static Color AccentText { get; private set; }
    public static Color Success { get; private set; }
    public static Color Warning { get; private set; }
    public static Color Danger { get; private set; }
    public static Color Info { get; private set; }
    public static Color Purple { get; private set; }
    public static Color Muted { get; private set; }

    // Поверхности — главное, что меняется между светлой и тёмной темой.
    public static Color WindowBackground { get; private set; }
    public static Color Surface { get; private set; }
    public static Color SurfaceMuted { get; private set; }

    // Текст.
    public static Color TextPrimary { get; private set; }
    public static Color TextSecondary { get; private set; }
    public static Color TextDisabled { get; private set; }

    // Линии и границы.
    public static Color Border { get; private set; }
    public static Color BorderMuted { get; private set; }

    // Подсветка строк (выделение, состояния).
    public static Color SelectionBackground { get; private set; }
    public static Color SelectionForeground { get; private set; }

    // Состояния строк гридов (просрочка, выигранная/проигранная сделка, завершённая задача).
    // В каждой теме подобраны под фон Surface, чтобы badge оставался читаемым.
    public static Color RowOverdueBackground { get; private set; }
    public static Color RowOverdueForeground { get; private set; }
    public static Color RowCompletedBackground { get; private set; }
    public static Color RowCompletedForeground { get; private set; }
    public static Color RowWonBackground { get; private set; }
    public static Color RowLostBackground { get; private set; }

    static AppPalette()
    {
        Apply(AppTheme.Light);
    }

    public static void Apply(AppTheme theme)
    {
        _theme = theme;
        if (theme == AppTheme.Dark)
        {
            ApplyDark();
        }
        else
        {
            ApplyLight();
        }
    }

    private static void ApplyLight()
    {
        Accent = Color.FromArgb(33, 150, 243);
        AccentText = Color.White;
        Success = Color.FromArgb(76, 175, 80);
        Warning = Color.FromArgb(255, 152, 0);
        Danger = Color.FromArgb(220, 53, 69);
        Info = Color.FromArgb(0, 188, 212);
        Purple = Color.FromArgb(156, 39, 176);
        Muted = Color.FromArgb(108, 117, 125);

        WindowBackground = Color.FromArgb(245, 246, 248);
        Surface = Color.White;
        SurfaceMuted = Color.FromArgb(248, 249, 250);

        TextPrimary = Color.FromArgb(33, 37, 41);
        TextSecondary = Color.FromArgb(73, 80, 87);
        TextDisabled = Color.FromArgb(150, 150, 150);

        Border = Color.FromArgb(225, 228, 232);
        BorderMuted = Color.FromArgb(180, 180, 180);

        SelectionBackground = Color.FromArgb(206, 230, 253);
        SelectionForeground = Color.Black;

        RowOverdueBackground = Color.FromArgb(252, 234, 232);
        RowOverdueForeground = Color.FromArgb(120, 30, 30);
        RowCompletedBackground = Color.FromArgb(245, 245, 245);
        RowCompletedForeground = Color.FromArgb(150, 150, 150);
        RowWonBackground = Color.FromArgb(232, 245, 233);
        RowLostBackground = Color.FromArgb(252, 234, 232);
    }

    private static void ApplyDark()
    {
        // Бренд оставляем — синий/зелёный/красный читаются на тёмном.
        Accent = Color.FromArgb(33, 150, 243);
        AccentText = Color.White;
        Success = Color.FromArgb(102, 187, 106);
        Warning = Color.FromArgb(255, 167, 38);
        Danger = Color.FromArgb(239, 83, 80);
        Info = Color.FromArgb(41, 182, 246);
        Purple = Color.FromArgb(186, 104, 200);
        Muted = Color.FromArgb(160, 160, 160);

        WindowBackground = Color.FromArgb(30, 30, 30);
        Surface = Color.FromArgb(45, 45, 48);
        SurfaceMuted = Color.FromArgb(55, 55, 58);

        TextPrimary = Color.FromArgb(220, 220, 220);
        TextSecondary = Color.FromArgb(180, 180, 180);
        TextDisabled = Color.FromArgb(120, 120, 120);

        Border = Color.FromArgb(70, 70, 75);
        BorderMuted = Color.FromArgb(90, 90, 95);

        SelectionBackground = Color.FromArgb(54, 81, 102);
        SelectionForeground = Color.White;

        // На тёмной теме нельзя использовать пастельные оттенки — текст в них не читается.
        // Берём более насыщенные тёмные тинты с яркими foreground'ами.
        RowOverdueBackground = Color.FromArgb(74, 38, 38);
        RowOverdueForeground = Color.FromArgb(255, 145, 145);
        RowCompletedBackground = Color.FromArgb(55, 55, 58);
        RowCompletedForeground = Color.FromArgb(140, 140, 140);
        RowWonBackground = Color.FromArgb(38, 60, 42);
        RowLostBackground = Color.FromArgb(74, 38, 38);
    }
}

public enum AppTheme
{
    Light,
    Dark,
}
