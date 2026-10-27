namespace PromotionMonitor.Domain.Entities;

public class UserAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? NotificationEmail { get; set; }
    public bool PreferEmailNotifications { get; set; } = true;
    public bool PreferConsoleNotifications { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Monitoring> Monitorings { get; set; } = new List<Monitoring>();
    public ICollection<NotificationDelivery> Deliveries { get; set; } = new List<NotificationDelivery>();
    public ICollection<CouponRule> CouponRules { get; set; } = new List<CouponRule>();
    public ICollection<CashbackRule> CashbackRules { get; set; } = new List<CashbackRule>();
}
