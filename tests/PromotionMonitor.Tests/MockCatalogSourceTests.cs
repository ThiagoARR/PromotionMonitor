using FluentAssertions;
using PromotionMonitor.Application.Abstractions;
using PromotionMonitor.Domain.Enums;
using PromotionMonitor.Infrastructure.Sources;

namespace PromotionMonitor.Tests;

public class MockCatalogSourceTests
{
    private readonly MockCatalogSource _sut = new();

    [Fact]
    public async Task Returns_offer_without_inventing_on_success()
    {
        var result = await _sut.FetchAsync(new PriceFetchRequest(
            Guid.NewGuid(),
            "https://example.com/products/demo-airfryer-110",
            "airfryer",
            Voltage.V110,
            "01310-100",
            DemoMode: true));

        result.Success.Should().BeTrue();
        result.ListedPrice.Should().NotBeNull();
        result.RawPayload.Should().NotBeNullOrWhiteSpace();
        result.VoltageMatches.Should().BeTrue();
    }

    [Fact]
    public async Task Failure_does_not_invent_prices()
    {
        var result = await _sut.FetchAsync(new PriceFetchRequest(
            Guid.NewGuid(),
            "https://example.com/products/fail-me",
            null,
            Voltage.Any,
            null,
            true));

        result.Success.Should().BeFalse();
        result.ListedPrice.Should().BeNull();
        result.ErrorMessage.Should().Contain("no price invented");
    }

    [Fact]
    public async Task Voltage_mismatch_is_reported()
    {
        var result = await _sut.FetchAsync(new PriceFetchRequest(
            Guid.NewGuid(),
            "https://example.com/products/demo-airfryer-220",
            null,
            Voltage.V110,
            null,
            true));

        result.Success.Should().BeTrue();
        result.VoltageMatches.Should().BeFalse();
    }
}
