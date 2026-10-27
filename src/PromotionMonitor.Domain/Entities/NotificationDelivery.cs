using PromotionMonitor.Domain.Enums;

namespace PromotionMonitor.Domain.Entities;

public class NotificationDelivery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public UserAccount? User { get; set; }
    public Guid MonitoringId { get; set; }
    public Monitoring? Monitoring { get; set; }
    public Guid? ObservationId { get; set; }

    public NotificationChannel Channel { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeliveredAt { get; set; }
}
