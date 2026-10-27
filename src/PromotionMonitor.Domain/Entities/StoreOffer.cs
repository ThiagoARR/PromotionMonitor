using PromotionMonitor.Domain.Enums;

namespace PromotionMonitor.Domain.Entities;

/// <summary>Snapshot of one store's offer in a comparison round.</summary>
public class StoreOffer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MonitoringId { get; set; }
    public Monitoring? Monitoring { get; set; }
    public Guid ComparisonRoundId { get; set; }
    public Guid? StoreLinkId { get; set; }

    public string StoreLabel { get; set; } = string.Empty;
    public string ProductUrl { get; set; } = string.Empty;
    public SourceKind SourceKind { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public DateTimeOffset CollectedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ProductTitle { get; set; }
    public decimal? ListedPrice { get; set; }
    public decimal? Shipping { get; set; }
    public string? ShippingProvider { get; set; }
    public bool ShippingSimulated { get; set; }
    public string? CouponCode { get; set; }
    public decimal? CouponDiscount { get; set; }
    public bool CouponConfirmed { get; set; }
    public decimal? CashbackEstimate { get; set; }
    public decimal? PayAtCheckout { get; set; }
    public decimal? NetAfterCashback { get; set; }
    public string? VoltageDetected { get; set; }
    public bool? VoltageMatches { get; set; }
    public string RawPayload { get; set; } = string.Empty;
    public bool IsBestPayAtCheckout { get; set; }
    public bool IsBestNetAfterCashback { get; set; }
}
