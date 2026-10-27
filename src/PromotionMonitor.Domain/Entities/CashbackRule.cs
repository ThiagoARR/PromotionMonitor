using PromotionMonitor.Domain.Enums;

namespace PromotionMonitor.Domain.Entities;

/// <summary>Cashback program — informational only; never reduces PayAtCheckout.</summary>
public class CashbackRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public UserAccount? User { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public DiscountKind Kind { get; set; } = DiscountKind.Percent;
    public decimal Value { get; set; }
    public decimal? MaxCashback { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
