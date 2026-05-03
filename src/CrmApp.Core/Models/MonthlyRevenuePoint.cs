using CrmApp.Core.Common;

namespace CrmApp.Core.Models;

// Точка на графике "выручка по месяцам".
public sealed record MonthlyRevenuePoint(int Year, int Month, Money Revenue);
