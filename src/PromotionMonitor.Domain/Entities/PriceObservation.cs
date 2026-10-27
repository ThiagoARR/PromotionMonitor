namespace PromotionMonitor.Domain.Entities;

public class PriceObservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MonitoringId { get; set; }
    public Monitoring? Monitoring { get; set; }

    public DateTimeOffset ObservedAt { get; set; } = DateTimeOffset.UtcNow;
    public string SourceName { get; set; } = string.Empty;
    public string RawPayload { get; set; } = string.Empty;
    public string NormalizedJson { get; set; } = string.Empty;

    public string? ProductTitle { get; set; }
    public decimal? ListedPrice { get; set; }
    public string Currency { get; set; } = "BRL";
    public string? VoltageDetected { get; set; }
    public bool? VoltageMatches { get; set; }
    public decimal? ShippingEstimate { get; set; }
    public string? CouponCode { get; set; }
    public decimal? CouponDiscount { get; set; }
    public decimal? CashbackEstimate { get; set; }

    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Fingerprint { get; set; }

    public PriceCalculation? Calculation { get; set; }
}
