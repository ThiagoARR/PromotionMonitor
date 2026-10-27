using FluentAssertions;
using PromotionMonitor.Application.Abstractions;
using PromotionMonitor.Application.Services;

namespace PromotionMonitor.Tests;

public class EffectiveCostCalculatorTests
{
    private readonly EffectiveCostCalculator _sut = new();

    [Fact]
    public void PayAtCheckout_includes_shipping_and_confirmed_coupon_but_not_cashback()
    {
        var result = _sut.Calculate(new CostInputs(
            ListedPrice: 400.00m,
            Shipping: 20.00m,
            CouponDiscount: 40.00m,
            CashbackEstimate: 50.00m,
            CouponConfirmed: true));

        result.PayAtCheckout.Should().Be(380.00m);
        result.CashbackEstimate.Should().Be(50.00m);
        result.NetAfterCashback.Should().Be(330.00m);
        result.PayAtCheckout.Should().BeGreaterThan(result.NetAfterCashback);
        result.Notes.Should().Contain("never reduces PayAtCheckout");
    }

    [Fact]
    public void Unconfirmed_coupon_is_ignored_at_checkout()
    {
        var result = _sut.Calculate(new CostInputs(
            ListedPrice: 100m,
            Shipping: 10m,
            CouponDiscount: 30m,
            CashbackEstimate: 5m,
            CouponConfirmed: false));

        result.CouponDiscount.Should().Be(0m);
        result.PayAtCheckout.Should().Be(110m);
        result.NetAfterCashback.Should().Be(105m);
    }

    [Fact]
    public void Coupon_cannot_exceed_listed_plus_shipping()
    {
        var result = _sut.Calculate(new CostInputs(50m, 10m, 999m, 0m, true));
        result.PayAtCheckout.Should().Be(0m);
        result.CouponDiscount.Should().Be(60m);
    }

    [Theory]
    [InlineData(-1, 0, 0, 0)]
    [InlineData(10, -1, 0, 0)]
    [InlineData(10, 0, -1, 0)]
    [InlineData(10, 0, 0, -1)]
    public void Negative_inputs_throw(decimal listed, decimal ship, decimal coupon, decimal cashback)
    {
        var act = () => _sut.Calculate(new CostInputs(listed, ship, coupon, cashback));
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Uses_decimal_precision_not_float()
    {
        var result = _sut.Calculate(new CostInputs(19.99m, 0.10m, 0.05m, 1.234m));
        result.PayAtCheckout.Should().Be(20.04m);
        result.CashbackEstimate.Should().Be(1.23m);
    }
}
