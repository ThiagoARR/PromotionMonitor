using PromotionMonitor.Domain.Enums;

namespace PromotionMonitor.Domain.Entities;

/// <summary>User-owned coupon that may reduce PayAtCheckout only when confirmed.</summary>
public class CouponRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public UserAccount? User { get; set; }

    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DiscountKind Kind { get; set; } = DiscountKind.FixedAmount;
    public decimal Value { get; set; }
    public decimal? MinSubtotal { get; set; }
    public decimal? MaxDiscount { get; set; }
    public bool IsConfirmed { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? ValidFrom { get; set; }
    public DateTimeOffset? ValidUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
