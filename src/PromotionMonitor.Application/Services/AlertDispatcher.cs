using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PromotionMonitor.Application.Abstractions;
using PromotionMonitor.Domain.Entities;
using PromotionMonitor.Domain.Enums;

namespace PromotionMonitor.Application.Services;

public interface IAlertDispatcher
{
    Task DispatchPriceAlertAsync(Monitoring monitoring, PriceObservation observation, CancellationToken cancellationToken = default);
}

public sealed class AlertDispatcher : IAlertDispatcher
{
    private readonly IAppDbContext _db;
    private readonly IEnumerable<INotificationProvider> _providers;
    private readonly ILogger<AlertDispatcher> _logger;

    public AlertDispatcher(
        IAppDbContext db,
        IEnumerable<INotificationProvider> providers,
        ILogger<AlertDispatcher> logger)
    {
        _db = db;
        _providers = providers;
        _logger = logger;
    }

    public async Task DispatchPriceAlertAsync(
        Monitoring monitoring,
        PriceObservation observation,
        CancellationToken cancellationToken = default)
    {
        var user = monitoring.User
            ?? await _db.Users.FirstAsync(u => u.Id == monitoring.UserId, cancellationToken);

        var calc = observation.Calculation
            ?? throw new InvalidOperationException("Observation missing calculation.");

        var subject = $"[PromotionMonitor] {monitoring.Name} ≤ R$ {calc.PayAtCheckout:F2}";
        var body =
            $"Product: {observation.ProductTitle ?? monitoring.Name}\n" +
            $"Pay at checkout: R$ {calc.PayAtCheckout:F2}\n" +
            $"  Listed: R$ {calc.ListedPrice:F2}\n" +
            $"  Shipping: R$ {calc.Shipping:F2}\n" +
            $"  Coupon: -R$ {calc.CouponDiscount:F2}\n" +
            $"Cashback estimate (not applied at checkout): R$ {calc.CashbackEstimate:F2}\n" +
            $"Net after cashback (informational): R$ {calc.NetAfterCashback:F2}\n" +
            $"URL: {monitoring.ProductUrl}\n" +
            $"Notes: {calc.Notes}";

        var channels = new List<NotificationChannel>();
        if (user.PreferConsoleNotifications)
            channels.Add(NotificationChannel.Console);
        if (user.PreferEmailNotifications)
            channels.Add(NotificationChannel.Email);

        foreach (var channel in channels)
        {
            var idempotencyKey =
                $"{monitoring.Id:N}:{observation.Fingerprint}:{channel}";

            var existing = await _db.Deliveries.FirstOrDefaultAsync(
                d => d.IdempotencyKey == idempotencyKey,
                cancellationToken);

            if (existing is not null)
            {
                _logger.LogInformation("Alert idempotency hit for key {Key}", idempotencyKey);
                continue;
            }

            var provider = _providers.FirstOrDefault(p => p.Channel == channel);
            if (provider is null)
            {
                _logger.LogWarning("No provider for channel {Channel}", channel);
                continue;
            }

            var delivery = new NotificationDelivery
            {
                UserId = user.Id,
                MonitoringId = monitoring.Id,
                ObservationId = observation.Id,
                Channel = channel,
                IdempotencyKey = idempotencyKey,
                Status = DeliveryStatus.Pending,
                Subject = subject,
                Body = body,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.Deliveries.Add(delivery);
            await _db.SaveChangesAsync(cancellationToken);

            var to = channel == NotificationChannel.Email
                ? (user.NotificationEmail ?? user.Email)
                : user.Email;

            try
            {
                var result = await provider.SendAsync(
                    new NotificationRequest(user.Id, monitoring.Id, observation.Id, to, subject, body, idempotencyKey),
                    cancellationToken);

                delivery.Status = result.Simulated
                    ? DeliveryStatus.Simulated
                    : result.Delivered
                        ? DeliveryStatus.Delivered
                        : DeliveryStatus.Failed;
                delivery.ErrorMessage = result.ErrorMessage;
                delivery.DeliveredAt = result.Delivered || result.Simulated ? DateTimeOffset.UtcNow : null;
            }
            catch (Exception ex)
            {
                delivery.Status = DeliveryStatus.Failed;
                delivery.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Notification failed for {Channel}", channel);
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
