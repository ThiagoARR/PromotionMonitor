using Microsoft.EntityFrameworkCore;
using PromotionMonitor.Application.Abstractions;
using PromotionMonitor.Domain.Enums;

namespace PromotionMonitor.Application.Services;

public sealed class CashbackEvaluator : ICashbackEvaluator
{
    private readonly IAppDbContext _db;

    public CashbackEvaluator(IAppDbContext db) => _db = db;

    public async Task<CashbackEvaluation> EvaluateBestAsync(
        Guid userId,
        decimal payAtCheckoutBasis,
        decimal? sourceSuggestedCashback = null,
        CancellationToken cancellationToken = default)
    {
        var rules = await _db.CashbackRules
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.IsActive)
            .ToListAsync(cancellationToken);

        CashbackEvaluation? best = null;
        foreach (var rule in rules)
        {
            var estimate = rule.Kind switch
            {
                DiscountKind.Percent => decimal.Round(
                    payAtCheckoutBasis * (rule.Value / 100m), 2, MidpointRounding.AwayFromZero),
                _ => rule.Value
            };
            if (rule.MaxCashback is decimal max && estimate > max)
                estimate = max;
            if (estimate < 0)
                estimate = 0;

            var candidate = new CashbackEvaluation(rule, estimate, rule.Name);
            if (best is null || candidate.Estimate > best.Estimate)
                best = candidate;
        }

        // User-configured programs win when present; otherwise fall back to source estimate.
        if (best is not null)
            return best;

        if (sourceSuggestedCashback is decimal src && src > 0)
            return new CashbackEvaluation(null, src, "source-estimate");

        return new CashbackEvaluation(null, 0m, null);
    }
}
