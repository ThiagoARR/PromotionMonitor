using PromotionMonitor.Domain.Entities;

namespace PromotionMonitor.Application.Abstractions;

public sealed record CostInputs(
    decimal ListedPrice,
    decimal Shipping = 0m,
    decimal CouponDiscount = 0m,
    decimal CashbackEstimate = 0m,
    bool CouponConfirmed = true,
    string? CouponCode = null,
    string? ShippingProvider = null,
    bool ShippingSimulated = false,
    string? CashbackProgram = null);

public interface IEffectiveCostCalculator
{
    PriceCalculation Calculate(CostInputs inputs);
}
