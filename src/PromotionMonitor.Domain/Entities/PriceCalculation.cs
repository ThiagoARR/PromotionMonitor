namespace PromotionMonitor.Domain.Entities;

/// <summary>
/// Auditable cost breakdown. Cashback never reduces PayAtCheckout.
/// </summary>
public class PriceCalculation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ObservationId { get; set; }
    public PriceObservation? Observation { get; set; }

    public decimal ListedPrice { get; set; }
    public decimal Shipping { get; set; }
    public string? ShippingProvider { get; set; }
    public bool ShippingSimulated { get; set; }
    public decimal CouponDiscount { get; set; }
    public string? CouponCode { get; set; }
    public bool CouponConfirmed { get; set; }

    /// <summary>Amount the shopper pays at checkout (listed + shipping − confirmed coupon).</summary>
    public decimal PayAtCheckout { get; set; }

    /// <summary>Estimated cashback; informational only — never reduces checkout.</summary>
    public decimal CashbackEstimate { get; set; }
    public string? CashbackProgram { get; set; }

    /// <summary>PayAtCheckout − CashbackEstimate for comparison; not a payable amount.</summary>
    public decimal NetAfterCashback { get; set; }

    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset CalculatedAt { get; set; } = DateTimeOffset.UtcNow;
}
