using PromotionMonitor.Domain.Entities;

namespace PromotionMonitor.Application.Abstractions;

public sealed record CouponEvaluation(
    CouponRule? Rule,
    decimal Discount,
    bool Confirmed,
    string? Code);

public interface ICouponEvaluator
{
    Task<CouponEvaluation> EvaluateBestAsync(
        Guid userId,
        decimal listedPrice,
        decimal shipping,
        string? sourceSuggestedCode = null,
        decimal? sourceSuggestedDiscount = null,
        CancellationToken cancellationToken = default);
}
