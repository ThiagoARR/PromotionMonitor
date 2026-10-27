using PromotionMonitor.Domain.Enums;

namespace PromotionMonitor.Domain.Entities;

public class Monitoring
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public UserAccount? User { get; set; }

    public string Name { get; set; } = string.Empty;
    public string ProductUrl { get; set; } = string.Empty;
    public string? ModelName { get; set; }
    public Voltage VoltageFilter { get; set; } = Voltage.Any;
    public decimal? TargetPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public string? Cep { get; set; }
    public int ScheduleMinutes { get; set; } = 60;
    public SourceKind SourceKind { get; set; } = SourceKind.Mock;
    public bool IsActive { get; set; } = true;
    public bool DemoMode { get; set; } = true;
    public DateTimeOffset? LastCheckedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<PriceObservation> Observations { get; set; } = new List<PriceObservation>();
    public ICollection<NotificationDelivery> Deliveries { get; set; } = new List<NotificationDelivery>();
    public ICollection<MonitoringStoreLink> StoreLinks { get; set; } = new List<MonitoringStoreLink>();
    public ICollection<StoreOffer> StoreOffers { get; set; } = new List<StoreOffer>();
}
