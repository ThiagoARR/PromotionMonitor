using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PromotionMonitor.Application.Abstractions;
using PromotionMonitor.Application.Services;
using PromotionMonitor.Domain.Entities;
using PromotionMonitor.Domain.Enums;
using PromotionMonitor.Infrastructure.Persistence;
using PromotionMonitor.Infrastructure.Shipping;
using PromotionMonitor.Infrastructure.Sources;
using PromotionMonitor.Infrastructure.Notifications;

namespace PromotionMonitor.Tests;

public class Phase2RulesTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Confirmed_user_coupon_beats_source_and_reduces_checkout_only()
    {
        await using var db = CreateDb();
        var user = new UserAccount { Email = "a@b.com", PasswordHash = "x", DisplayName = "A" };
        db.Users.Add(user);
        db.CouponRules.Add(new CouponRule
        {
            UserId = user.Id,
            Code = "SAVE50",
            Kind = DiscountKind.FixedAmount,
            Value = 50m,
            IsConfirmed = true,
            IsActive = true
        });
        db.CashbackRules.Add(new CashbackRule
        {
            UserId = user.Id,
            Name = "Portal 5%",
            Kind = DiscountKind.Percent,
            Value = 5m,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var coupons = new CouponEvaluator(db);
        var cashback = new CashbackEvaluator(db);
        var calc = new EffectiveCostCalculator();

        var couponEval = await coupons.EvaluateBestAsync(user.Id, 400m, 20m, "AIR10", 40m);
        couponEval.Code.Should().Be("SAVE50");
        couponEval.Discount.Should().Be(50m);
        couponEval.Confirmed.Should().BeTrue();

        var first = calc.Calculate(new CostInputs(400m, 20m, couponEval.Discount, 0m, true, couponEval.Code));
        first.PayAtCheckout.Should().Be(370m);

        var cb = await cashback.EvaluateBestAsync(user.Id, first.PayAtCheckout, 20m);
        cb.Estimate.Should().Be(18.50m); // 5% of 370

        var final = calc.Calculate(new CostInputs(
            400m, 20m, couponEval.Discount, cb.Estimate, true, couponEval.Code, CashbackProgram: cb.ProgramName));
        final.PayAtCheckout.Should().Be(370m);
        final.CashbackEstimate.Should().Be(18.50m);
        final.NetAfterCashback.Should().Be(351.50m);
        final.PayAtCheckout.Should().Be(final.NetAfterCashback + final.CashbackEstimate);
    }

    [Fact]
    public async Task Unconfirmed_coupon_does_not_reduce_checkout()
    {
        await using var db = CreateDb();
        var user = new UserAccount { Email = "c@d.com", PasswordHash = "x", DisplayName = "C" };
        db.Users.Add(user);
        db.CouponRules.Add(new CouponRule
        {
            UserId = user.Id,
            Code = "MAYBE",
            Kind = DiscountKind.FixedAmount,
            Value = 100m,
            IsConfirmed = false,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var eval = await new CouponEvaluator(db).EvaluateBestAsync(user.Id, 200m, 0m);
        eval.Discount.Should().Be(100m);
        eval.Confirmed.Should().BeFalse();

        var calc = new EffectiveCostCalculator().Calculate(
            new CostInputs(200m, 0m, eval.Discount, 10m, eval.Confirmed, eval.Code));
        calc.PayAtCheckout.Should().Be(200m);
        calc.CouponDiscount.Should().Be(0m);
        calc.NetAfterCashback.Should().Be(190m);
    }

    [Fact]
    public async Task Simulated_shipping_quotes_by_cep_and_rejects_invalid()
    {
        var sut = new SimulatedShippingProvider();
        var ok = await sut.QuoteAsync(new ShippingQuoteRequest("01310-100", 400m));
        ok.Success.Should().BeTrue();
        ok.Simulated.Should().BeTrue();
        ok.Amount.Should().NotBeNull();

        var bad = await sut.QuoteAsync(new ShippingQuoteRequest("123", 100m));
        bad.Success.Should().BeFalse();
        bad.Amount.Should().BeNull();
        bad.ErrorMessage.Should().Contain("not invented");
    }

    [Fact]
    public async Task Http_shipping_without_credentials_does_not_invent()
    {
        var http = new HttpShippingProvider(
            Options.Create(new ShippingApiOptions()),
            new FakeHttpFactory(),
            NullLogger<HttpShippingProvider>.Instance);

        var result = await http.QuoteAsync(new ShippingQuoteRequest("01310100", 100m));
        result.Success.Should().BeFalse();
        result.Amount.Should().BeNull();
        result.ErrorMessage.Should().Contain("credentials missing");
    }

    [Fact]
    public async Task Multi_store_comparison_marks_best_pay_at_checkout()
    {
        await using var db = CreateDb();
        var user = new UserAccount
        {
            Email = $"m-{Guid.NewGuid():N}@e.com",
            PasswordHash = "x",
            DisplayName = "M",
            PreferConsoleNotifications = true,
            PreferEmailNotifications = false
        };
        db.Users.Add(user);
        var monitoring = new Monitoring
        {
            UserId = user.Id,
            Name = "Compare",
            ProductUrl = "https://example.com/products/demo-airfryer-110",
            MaxPrice = 9999m,
            Cep = "01310100",
            SourceKind = SourceKind.Mock,
            DemoMode = true,
            IsActive = true,
            User = user
        };
        monitoring.StoreLinks.Add(new MonitoringStoreLink
        {
            Label = "Store A",
            ProductUrl = "https://example.com/products/demo-airfryer-110",
            SourceKind = SourceKind.Mock,
            IsPrimary = true
        });
        monitoring.StoreLinks.Add(new MonitoringStoreLink
        {
            Label = "Store B",
            ProductUrl = "https://example.com/products/demo-airfryer-220",
            SourceKind = SourceKind.Mock,
            IsPrimary = false
        });
        db.Monitorings.Add(monitoring);
        await db.SaveChangesAsync();

        var collector = new MonitoringCollector(
            db,
            new IPriceSource[] { new MockCatalogSource() },
            new EffectiveCostCalculator(),
            new CouponEvaluator(db),
            new CashbackEvaluator(db),
            new SimulatedShippingProvider(),
            new AlertDispatcher(
                db,
                new INotificationProvider[] { new ConsoleNotificationProvider(NullLogger<ConsoleNotificationProvider>.Instance) },
                NullLogger<AlertDispatcher>.Instance),
            NullLogger<MonitoringCollector>.Instance);

        await collector.CollectAsync(monitoring.Id);

        var offers = await db.StoreOffers.Where(o => o.MonitoringId == monitoring.Id).ToListAsync();
        offers.Should().HaveCount(2);
        offers.Count(o => o.Success).Should().Be(2);
        offers.Count(o => o.IsBestPayAtCheckout).Should().BeGreaterThan(0);
        offers.Where(o => o.Success).Should().OnlyContain(o => o.Shipping != null);
    }

    private sealed class FakeHttpFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
