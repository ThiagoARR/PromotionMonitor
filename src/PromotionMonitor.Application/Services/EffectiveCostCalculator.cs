using PromotionMonitor.Application.Abstractions;
using PromotionMonitor.Domain.Entities;

namespace PromotionMonitor.Application.Services;

public sealed class EffectiveCostCalculator : IEffectiveCostCalculator
{
    public PriceCalculation Calculate(CostInputs inputs)
    {
        if (inputs.ListedPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(inputs.ListedPrice), "Listed price cannot be negative.");
        if (inputs.Shipping < 0)
            throw new ArgumentOutOfRangeException(nameof(inputs.Shipping), "Shipping cannot be negative.");
        if (inputs.CouponDiscount < 0)
            throw new ArgumentOutOfRangeException(nameof(inputs.CouponDiscount), "Coupon discount cannot be negative.");
        if (inputs.CashbackEstimate < 0)
            throw new ArgumentOutOfRangeException(nameof(inputs.CashbackEstimate), "Cashback cannot be negative.");

        var coupon = inputs.CouponConfirmed ? inputs.CouponDiscount : 0m;
        if (coupon > inputs.ListedPrice + inputs.Shipping)
            coupon = inputs.ListedPrice + inputs.Shipping;

        var payAtCheckout = decimal.Round(inputs.ListedPrice + inputs.Shipping - coupon, 2, MidpointRounding.AwayFromZero);
        var cashback = decimal.Round(inputs.CashbackEstimate, 2, MidpointRounding.AwayFromZero);
        var netAfter = decimal.Round(payAtCheckout - cashback, 2, MidpointRounding.AwayFromZero);

        var notes = inputs.CouponConfirmed
            ? "Coupon applied to checkout. Cashback is estimated and never reduces PayAtCheckout."
            : "Coupon not confirmed; ignored at checkout. Cashback is estimated and never reduces PayAtCheckout.";
        if (inputs.ShippingSimulated)
            notes += " Shipping quote is simulated.";
        else if (!string.IsNullOrWhiteSpace(inputs.ShippingProvider))
            notes += $" Shipping via {inputs.ShippingProvider}.";

        return new PriceCalculation
        {
            ListedPrice = decimal.Round(inputs.ListedPrice, 2, MidpointRounding.AwayFromZero),
            Shipping = decimal.Round(inputs.Shipping, 2, MidpointRounding.AwayFromZero),
            ShippingProvider = inputs.ShippingProvider,
            ShippingSimulated = inputs.ShippingSimulated,
            CouponDiscount = decimal.Round(coupon, 2, MidpointRounding.AwayFromZero),
            CouponCode = inputs.CouponCode,
            CouponConfirmed = inputs.CouponConfirmed && coupon > 0,
            PayAtCheckout = payAtCheckout,
            CashbackEstimate = cashback,
            CashbackProgram = inputs.CashbackProgram,
            NetAfterCashback = netAfter,
            Notes = notes,
            CalculatedAt = DateTimeOffset.UtcNow
        };
    }
}
