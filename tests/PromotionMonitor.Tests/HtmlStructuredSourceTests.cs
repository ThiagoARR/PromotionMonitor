using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PromotionMonitor.Application.Abstractions;
using PromotionMonitor.Application.Services;
using PromotionMonitor.Domain.Enums;
using PromotionMonitor.Infrastructure.Sources;

namespace PromotionMonitor.Tests;

public class HtmlStructuredSourceTests
{
    private sealed class FakeFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    [Fact]
    public async Task Demo_mode_returns_structured_offer_with_coupon()
    {
        var sut = new HtmlStructuredSource(
            new FakeFactory(),
            new UrlSafetyValidator(),
            NullLogger<HtmlStructuredSource>.Instance);

        var result = await sut.FetchAsync(new PriceFetchRequest(
            Guid.NewGuid(),
            "https://example.com/p/1",
            "Blender 110V",
            Voltage.V110,
            "01310100",
            DemoMode: true));

        result.Success.Should().BeTrue();
        result.ListedPrice.Should().Be(599.90m);
        result.CouponCode.Should().Be("DEMO5");
        result.CouponDiscount.Should().Be(30.00m);
        result.RawPayload.Should().Contain("jsonLd");
    }

    [Fact]
    public async Task Live_mode_rejects_unsafe_url_without_inventing_price()
    {
        var sut = new HtmlStructuredSource(
            new FakeFactory(),
            new UrlSafetyValidator(),
            NullLogger<HtmlStructuredSource>.Instance);

        var result = await sut.FetchAsync(new PriceFetchRequest(
            Guid.NewGuid(),
            "http://127.0.0.1/secret",
            null,
            Voltage.Any,
            null,
            DemoMode: false));

        result.Success.Should().BeFalse();
        result.ListedPrice.Should().BeNull();
    }
}
