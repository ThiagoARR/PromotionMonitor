using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PromotionMonitor.Application.Abstractions;
using PromotionMonitor.Domain.Enums;

namespace PromotionMonitor.Infrastructure.Notifications;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? From { get; set; }
    public bool EnableSsl { get; set; } = true;
}

/// <summary>
/// Sends via SMTP when configured; otherwise clearly simulates delivery (no silent fake success as real).
/// </summary>
public sealed class EmailNotificationProvider : INotificationProvider
{
    private readonly SmtpOptions _options;
    private readonly ILogger<EmailNotificationProvider> _logger;

    public EmailNotificationProvider(IOptions<SmtpOptions> options, ILogger<EmailNotificationProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.Email;

    public async Task<NotificationResult> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.From))
        {
            _logger.LogWarning(
                "SMTP credentials missing — SIMULATED email to={To} subject={Subject} key={Key}",
                request.ToAddress,
                request.Subject,
                request.IdempotencyKey);
            return new NotificationResult(Delivered: true, Simulated: true, ErrorMessage: "SMTP not configured; simulated.");
        }

        try
        {
            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                Credentials = string.IsNullOrWhiteSpace(_options.Username)
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(_options.Username, _options.Password)
            };

            using var message = new MailMessage(_options.From, request.ToAddress, request.Subject, request.Body);
            await client.SendMailAsync(message, cancellationToken);
            return new NotificationResult(Delivered: true, Simulated: false, ErrorMessage: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed");
            return new NotificationResult(Delivered: false, Simulated: false, ErrorMessage: ex.Message);
        }
    }
}

/// <summary>Phase 3 stub — interface reserved, not registered for delivery in MVP.</summary>
public sealed class WhatsAppNotificationStub : INotificationProvider
{
    public NotificationChannel Channel => NotificationChannel.WhatsApp;

    public Task<NotificationResult> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new NotificationResult(false, true, "WhatsApp reserved for Phase 3."));
}
