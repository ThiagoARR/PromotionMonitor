using PromotionMonitor.Domain.Enums;

namespace PromotionMonitor.Domain.Entities;

/// <summary>Alternate store URL for multi-store comparison on a monitoring.</summary>
public class MonitoringStoreLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MonitoringId { get; set; }
    public Monitoring? Monitoring { get; set; }

    public string Label { get; set; } = string.Empty;
    public string ProductUrl { get; set; } = string.Empty;
    public SourceKind SourceKind { get; set; } = SourceKind.Mock;
    public bool IsPrimary { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
