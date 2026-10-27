namespace PromotionMonitor.Domain.Enums;

public enum DeliveryStatus
{
    Pending = 0,
    Delivered = 1,
    Simulated = 2,
    Failed = 3,
    SkippedDuplicate = 4
}
