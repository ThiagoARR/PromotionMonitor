using PromotionMonitor.Domain.Entities;

namespace PromotionMonitor.Application.Abstractions;

public sealed record CashbackEvaluation(
    CashbackRule? Rule,
    decimal Estimate,
    string? ProgramName);

public interface ICashbackEvaluator
{
    Task<CashbackEvaluation> EvaluateBestAsync(
        Guid userId,
        decimal payAtCheckoutBasis,
        decimal? sourceSuggestedCashback = null,
        CancellationToken cancellationToken = default);
}
