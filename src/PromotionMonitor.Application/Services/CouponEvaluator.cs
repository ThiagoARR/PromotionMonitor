using Microsoft.EntityFrameworkCore;
using PromotionMonitor.Application.Abstractions;
using PromotionMonitor.Domain.Enums;

namespace PromotionMonitor.Application.Services;

public sealed class CouponEvaluator : ICouponEvaluator
{
    private readonly IAppDbContext _db;

    public CouponEvaluator(IAppDbContext db) => _db = db;

    public async Task<CouponEvaluation> EvaluateBestAsync(
        Guid userId,
        decimal listedPrice,
        decimal shipping,
        string? sourceSuggestedCode = null,
        decimal? sourceSuggestedDiscount = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var subtotal = listedPrice + shipping;
        var rules = await _db.CouponRules
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.IsActive)
            .Where(r => r.ValidFrom == null || r.ValidFrom <= now)
            .Where(r => r.ValidUntil == null || r.ValidUntil >= now)
            .ToListAsync(cancellationToken);

        CouponEvaluation? best = null;

        foreach (var rule in rules)
        {
            if (rule.MinSubtotal is decimal min && subtotal < min)
                continue;

            var discount = rule.Kind switch
            {
                DiscountKind.Percent => decimal.Round(subtotal * (rule.Value / 100m), 2, MidpointRounding.AwayFromZero),
                _ => rule.Value
            };

            if (rule.MaxDiscount is decimal max && discount > max)
                discount = max;
            if (discount > subtotal)
                discount = subtotal;
            if (discount <= 0)
                continue;

            var candidate = new CouponEvaluation(rule, discount, rule.IsConfirmed, rule.Code);
            if (best is null || Better(candidate, best))
                best = candidate;
        }

        // Fall back to source-suggested coupon when no user rule wins.
        if (sourceSuggestedDiscount is decimal srcDisc && srcDisc > 0)
        {
            var capped = Math.Min(srcDisc, subtotal);
            var sourceEval = new CouponEvaluation(
                null,
                capped,
                Confirmed: !string.IsNullOrWhiteSpace(sourceSuggestedCode),
                Code: sourceSuggestedCode);

            if (best is null || Better(sourceEval, best))
                best = sourceEval;
        }

        return best ?? new CouponEvaluation(null, 0m, false, null);
    }

    /// <summary>Prefer higher confirmed discount; confirmed beats unconfirmed at equal amount.</summary>
    private static bool Better(CouponEvaluation a, CouponEvaluation b)
    {
        var aEffective = a.Confirmed ? a.Discount : 0m;
        var bEffective = b.Confirmed ? b.Discount : 0m;
        if (aEffective != bEffective)
            return aEffective > bEffective;
        if (a.Confirmed != b.Confirmed)
            return a.Confirmed;
        return a.Discount > b.Discount;
    }
}
