namespace CrmApp.WinForms.Theming;

// Глобальный значок приложения для всех Form'ов.
// Берётся из Win32-ресурсов exe (тот самый icon.ico, что указан в csproj
// через ApplicationIcon). Кешируется, чтобы не дёргать диск для каждой формы.
//
// По умолчанию WinForms ставит на Form встроенную системную иконку, и в taskbar/
// заголовке окна показывается дефолтный значок Windows вместо нашего бренда.
// Установка Form.Icon в конструкторе исправляет это.
public static class AppIcon
{
    private static Icon? _cached;

    public static Icon Default
    {
        get
        {
            if (_cached is not null) return _cached;
            try
            {
                _cached = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
            }
            catch
            {
                _cached = SystemIcons.Application;
            }
            return _cached;
        }
    }
}
