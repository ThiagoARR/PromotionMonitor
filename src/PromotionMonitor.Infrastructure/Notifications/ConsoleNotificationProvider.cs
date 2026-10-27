using Microsoft.Extensions.Logging;
using PromotionMonitor.Application.Abstractions;
using PromotionMonitor.Domain.Enums;

namespace PromotionMonitor.Infrastructure.Notifications;

/// <summary>
/// Dev/console provider — always available; logs and reports simulated/delivered for local demos.
/// </summary>
public sealed class ConsoleNotificationProvider : INotificationProvider
{
    private readonly ILogger<ConsoleNotificationProvider> _logger;

    public ConsoleNotificationProvider(ILogger<ConsoleNotificationProvider> logger)
    {
        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.Console;

    public Task<NotificationResult> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DEV NOTIFICATION to={To} subject={Subject} key={Key}\n{Body}",
            request.ToAddress,
            request.Subject,
            request.IdempotencyKey,
            request.Body);

        return Task.FromResult(new NotificationResult(Delivered: true, Simulated: true, ErrorMessage: null));
    }
}
