namespace PromotionMonitor.Domain.Entities;

/// <summary>Auditable shipping quote for a CEP (simulated or live provider).</summary>
public class ShippingQuoteRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? MonitoringId { get; set; }
    public string Cep { get; set; } = string.Empty;
    public decimal ListedPrice { get; set; }
    public decimal? QuoteAmount { get; set; }
    public string Provider { get; set; } = string.Empty;
    public bool Simulated { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string RawPayload { get; set; } = "{}";
    public DateTimeOffset QuotedAt { get; set; } = DateTimeOffset.UtcNow;
}
