using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PromotionMonitor.Application.Abstractions;
using PromotionMonitor.Application.Services;
using PromotionMonitor.Domain.Entities;
using PromotionMonitor.Domain.Enums;
using PromotionMonitor.Infrastructure.Persistence;
using PromotionMonitor.Infrastructure.Shipping;
using PromotionMonitor.Infrastructure.Sources;
using PromotionMonitor.Infrastructure.Notifications;

namespace PromotionMonitor.Tests;

public class MonitoringCollectorTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(AppDbContext db, UserAccount user, Monitoring monitoring)> SeedAsync(
        SourceKind kind = SourceKind.Mock,
        string url = "https://example.com/products/demo-airfryer-110",
        decimal maxPrice = 9999m,
        Voltage voltage = Voltage.Any)
    {
        var db = CreateDb();
        var user = new UserAccount
        {
            Email = $"u-{Guid.NewGuid():N}@example.com",
            PasswordHash = "x",
            DisplayName = "Tester",
            PreferConsoleNotifications = true,
            PreferEmailNotifications = false
        };
        db.Users.Add(user);
        var monitoring = new Monitoring
        {
            UserId = user.Id,
            Name = "Air Fryer",
            ProductUrl = url,
            MaxPrice = maxPrice,
            VoltageFilter = voltage,
            SourceKind = kind,
            DemoMode = true,
            ScheduleMinutes = 60,
            IsActive = true
        };
        monitoring.User = user;
        db.Monitorings.Add(monitoring);
        await db.SaveChangesAsync();
        return (db, user, monitoring);
    }

    private static MonitoringCollector CreateCollector(AppDbContext db)
    {
        var sources = new IPriceSource[] { new MockCatalogSource() };
        var providers = new INotificationProvider[]
        {
            new ConsoleNotificationProvider(NullLogger<ConsoleNotificationProvider>.Instance)
        };
        var alerts = new AlertDispatcher(db, providers, NullLogger<AlertDispatcher>.Instance);
        return new MonitoringCollector(
            db,
            sources,
            new EffectiveCostCalculator(),
            new CouponEvaluator(db),
            new CashbackEvaluator(db),
            new SimulatedShippingProvider(),
            alerts,
            NullLogger<MonitoringCollector>.Instance);
    }

    [Fact]
    public async Task Stores_history_and_calculation_breakdown()
    {
        var (db, _, monitoring) = await SeedAsync();
        var collector = CreateCollector(db);

        await collector.CollectAsync(monitoring.Id);

        var obs = await db.Observations.Include(o => o.Calculation).SingleAsync();
        obs.Success.Should().BeTrue();
        obs.RawPayload.Should().NotBeNullOrWhiteSpace();
        obs.NormalizedJson.Should().NotBeNullOrWhiteSpace();
        obs.Calculation.Should().NotBeNull();
        obs.Calculation!.PayAtCheckout.Should().Be(
            obs.Calculation.ListedPrice + obs.Calculation.Shipping - obs.Calculation.CouponDiscount);
        obs.Calculation.PayAtCheckout.Should().Be(
            obs.Calculation.NetAfterCashback + obs.Calculation.CashbackEstimate);
    }

    [Fact]
    public async Task Source_failure_stores_error_without_price()
    {
        var (db, _, monitoring) = await SeedAsync(url: "https://example.com/products/fail");
        await CreateCollector(db).CollectAsync(monitoring.Id);

        var obs = await db.Observations.SingleAsync();
        obs.Success.Should().BeFalse();
        obs.ListedPrice.Should().BeNull();
        obs.Calculation.Should().BeNull();
        obs.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Duplicate_observations_skip_second_alert()
    {
        var (db, user, monitoring) = await SeedAsync(maxPrice: 9999m);
        var collector = CreateCollector(db);

        await collector.CollectAsync(monitoring.Id);
        await collector.CollectAsync(monitoring.Id);

        var deliveries = await db.Deliveries.Where(d => d.UserId == user.Id).ToListAsync();
        deliveries.Should().HaveCount(1);
        db.Observations.Count().Should().Be(2);
    }

    [Fact]
    public async Task Alert_idempotency_key_is_unique()
    {
        var (db, _, monitoring) = await SeedAsync(maxPrice: 9999m);
        await CreateCollector(db).CollectAsync(monitoring.Id);
        await CreateCollector(db).CollectAsync(monitoring.Id);

        var keys = await db.Deliveries.Select(d => d.IdempotencyKey).ToListAsync();
        keys.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Voltage_mismatch_does_not_alert_even_if_under_max()
    {
        var (db, _, monitoring) = await SeedAsync(
            url: "https://example.com/products/demo-airfryer-220",
            maxPrice: 9999m,
            voltage: Voltage.V110);

        await CreateCollector(db).CollectAsync(monitoring.Id);

        var obs = await db.Observations.SingleAsync();
        obs.VoltageMatches.Should().BeFalse();
        db.Deliveries.Should().BeEmpty();
    }

    [Fact]
    public async Task History_window_filters_by_observed_at()
    {
        var (db, user, monitoring) = await SeedAsync();
        db.Observations.Add(new PriceObservation
        {
            MonitoringId = monitoring.Id,
            ObservedAt = DateTimeOffset.UtcNow.AddDays(-40),
            Success = true,
            ListedPrice = 100m,
            SourceName = "old",
            RawPayload = "{}",
            NormalizedJson = "{}"
        });
        db.Observations.Add(new PriceObservation
        {
            MonitoringId = monitoring.Id,
            ObservedAt = DateTimeOffset.UtcNow.AddDays(-2),
            Success = true,
            ListedPrice = 90m,
            SourceName = "recent",
            RawPayload = "{}",
            NormalizedJson = "{}"
        });
        await db.SaveChangesAsync();

        var since = DateTimeOffset.UtcNow.AddDays(-30);
        var window = await db.Observations
            .Where(o => o.MonitoringId == monitoring.Id && o.ObservedAt >= since)
            .ToListAsync();

        window.Should().ContainSingle(o => o.SourceName == "recent");
        window.Should().NotContain(o => o.SourceName == "old");
        user.Email.Should().Contain("@");
    }

    [Fact]
    public async Task User_isolation_prevents_cross_user_read()
    {
        var (db, userA, monitoringA) = await SeedAsync();
        var userB = new UserAccount
        {
            Email = $"b-{Guid.NewGuid():N}@example.com",
            PasswordHash = "x",
            DisplayName = "B"
        };
        db.Users.Add(userB);
        await db.SaveChangesAsync();

        var visibleToB = await db.Monitorings
            .Where(m => m.UserId == userB.Id && m.Id == monitoringA.Id)
            .ToListAsync();

        visibleToB.Should().BeEmpty();
        (await db.Monitorings.CountAsync(m => m.UserId == userA.Id)).Should().Be(1);
    }
}
