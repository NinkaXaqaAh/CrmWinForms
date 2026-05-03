using CrmApp.Core.Abstractions;
using CrmApp.Core.Common;
using CrmApp.Core.Enums;
using CrmApp.Core.Exceptions;
using CrmApp.Core.Models;
using Microsoft.Extensions.Logging;

namespace CrmApp.Infrastructure.Services;

// Доменный сервис воронки.
public sealed class DealPipelineService : IDealPipelineService
{
    // Допустимые переходы по этапам воронки.
    // Принципы:
    //  - вперёд можно с любого открытого этапа на любой следующий открытый;
    //  - закрыть (Won/Lost) можно с любого открытого;
    //  - возврат с Won/Lost запрещён;
    //  - на Этапе 4 можно расширить (например, вернуть ошибочно закрытую сделку с правом Admin).
    private static readonly Dictionary<DealStage, HashSet<DealStage>> AllowedTransitions = new()
    {
        [DealStage.New] = [DealStage.Qualified, DealStage.Proposal, DealStage.Negotiation, DealStage.Won, DealStage.Lost],
        [DealStage.Qualified] = [DealStage.Proposal, DealStage.Negotiation, DealStage.Won, DealStage.Lost],
        [DealStage.Proposal] = [DealStage.Qualified, DealStage.Negotiation, DealStage.Won, DealStage.Lost],
        [DealStage.Negotiation] = [DealStage.Proposal, DealStage.Won, DealStage.Lost],
        [DealStage.Won] = [],
        [DealStage.Lost] = [],
    };

    private readonly IDealRepository _deals;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<DealPipelineService> _logger;

    public DealPipelineService(
        IDealRepository deals,
        IDateTimeProvider clock,
        ILogger<DealPipelineService> logger)
    {
        ArgumentNullException.ThrowIfNull(deals);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);

        _deals = deals;
        _clock = clock;
        _logger = logger;
    }

    public async Task MoveToStageAsync(Guid dealId, DealStage newStage, CancellationToken ct = default)
    {
        var deal = await _deals.GetByIdAsync(dealId, ct).ConfigureAwait(false);

        if (deal.Stage == newStage)
        {
            return; // Идемпотентность - повторный перевод на тот же этап ничего не делает.
        }

        if (!AllowedTransitions[deal.Stage].Contains(newStage))
        {
            throw new DomainException(
                $"Нельзя перевести сделку с этапа \"{deal.Stage}\" на \"{newStage}\"");
        }

        deal.Stage = newStage;

        // При закрытии фиксируем дату и приводим вероятность к 100% / 0%.
        if (newStage == DealStage.Won)
        {
            deal.ActualCloseDate = _clock.Today;
            deal.Probability = 100;
        }
        else if (newStage == DealStage.Lost)
        {
            deal.ActualCloseDate = _clock.Today;
            deal.Probability = 0;
        }

        await _deals.UpdateAsync(deal, ct).ConfigureAwait(false);
        _logger.LogInformation("Сделка {DealId} переведена на этап {Stage}", dealId, newStage);
    }

    public async Task<Money> ForecastRevenueAsync(
        DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
    {
        var deals = await _deals.GetClosingInPeriodAsync(startDate, endDate, ct).ConfigureAwait(false);

        var weighted = deals
            .Where(d => !d.IsClosed)
            .Sum(d => d.Amount.Amount * d.Probability / 100m);

        // Используем валюту первой сделки или RUB по умолчанию.
        var currency = deals.FirstOrDefault()?.Amount.Currency ?? "RUB";
        return new Money(weighted, currency);
    }

    public async Task<IReadOnlyDictionary<DealStage, IReadOnlyList<Deal>>> GetPipelineAsync(
        CancellationToken ct = default)
    {
        var all = await _deals.GetAllAsync(ct).ConfigureAwait(false);

        // Возвращаем все этапы, даже если на них нет сделок - чтобы UI мог показать пустые колонки.
        var result = new Dictionary<DealStage, IReadOnlyList<Deal>>();
        foreach (var stage in Enum.GetValues<DealStage>())
        {
            result[stage] = all.Where(d => d.Stage == stage).ToList();
        }
        return result;
    }

    public async Task<IReadOnlyDictionary<DealStage, double>> GetConversionRatesAsync(
        CancellationToken ct = default)
    {
        var all = await _deals.GetAllAsync(ct).ConfigureAwait(false);
        var byStage = all.GroupBy(d => d.Stage).ToDictionary(g => g.Key, g => g.Count());

        // Конверсия каждого этапа = (число сделок, прошедших дальше) / (число сделок, побывавших на этапе).
        // Упрощённая модель: считаем что число "побывавших на этапе" = количество на нём + дальше + Won + Lost.
        // Для обучения этого достаточно; для прод-CRM нужна история переходов.
        var ordered = new[] { DealStage.New, DealStage.Qualified, DealStage.Proposal, DealStage.Negotiation };
        var result = new Dictionary<DealStage, double>();

        for (var i = 0; i < ordered.Length; i++)
        {
            var atOrAfter = ordered.Skip(i).Sum(s => byStage.GetValueOrDefault(s, 0))
                + byStage.GetValueOrDefault(DealStage.Won, 0)
                + byStage.GetValueOrDefault(DealStage.Lost, 0);
            var after = ordered.Skip(i + 1).Sum(s => byStage.GetValueOrDefault(s, 0))
                + byStage.GetValueOrDefault(DealStage.Won, 0)
                + byStage.GetValueOrDefault(DealStage.Lost, 0);

            result[ordered[i]] = atOrAfter == 0 ? 0.0 : (double)after / atOrAfter * 100.0;
        }

        return result;
    }
}
