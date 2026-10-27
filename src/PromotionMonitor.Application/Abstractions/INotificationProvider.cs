using PromotionMonitor.Domain.Enums;

namespace PromotionMonitor.Application.Abstractions;

public sealed record NotificationRequest(
    Guid UserId,
    Guid MonitoringId,
    Guid? ObservationId,
    string ToAddress,
    string Subject,
    string Body,
    string IdempotencyKey);

public sealed record NotificationResult(
    bool Delivered,
    bool Simulated,
    string? ErrorMessage);

public interface INotificationProvider
{
    NotificationChannel Channel { get; }
    Task<NotificationResult> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default);
}
