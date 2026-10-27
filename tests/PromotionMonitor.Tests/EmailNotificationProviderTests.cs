using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PromotionMonitor.Application.Abstractions;
using PromotionMonitor.Infrastructure.Notifications;

namespace PromotionMonitor.Tests;

public class EmailNotificationProviderTests
{
    [Fact]
    public async Task Missing_smtp_credentials_simulate_delivery()
    {
        var sut = new EmailNotificationProvider(
            Options.Create(new SmtpOptions()),
            NullLogger<EmailNotificationProvider>.Instance);

        var result = await sut.SendAsync(new NotificationRequest(
            Guid.NewGuid(), Guid.NewGuid(), null, "a@b.com", "subj", "body", "key-1"));

        result.Simulated.Should().BeTrue();
        result.Delivered.Should().BeTrue();
        result.ErrorMessage.Should().Contain("SMTP not configured");
    }
}
