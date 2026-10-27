using System.Text.Json;
using PromotionMonitor.Application.Abstractions;
using PromotionMonitor.Application.Services;
using PromotionMonitor.Domain.Enums;

namespace PromotionMonitor.Infrastructure.Sources;

/// <summary>
/// Deterministic demo catalog for CI and local use without hitting live retailers.
/// URLs containing "fail" simulate source failure (no invented prices).
/// </summary>
public sealed class MockCatalogSource : IPriceSource
{
    public SourceKind Kind => SourceKind.Mock;

    private static readonly IReadOnlyList<DemoOffer> Catalog =
    [
        new("demo-airfryer-110", "Air Fryer Crisp 4L 110V", 399.90m, "110V", 19.90m, "AIR10", 40.00m, 20.00m),
        new("demo-airfryer-220", "Air Fryer Crisp 4L 220V", 389.90m, "220V", 19.90m, "AIR10", 40.00m, 19.00m),
        new("demo-airfryer-bivolt", "Air Fryer Crisp 4L Bivolt", 429.90m, "Bivolt", 0m, null, 0m, 21.00m),
        new("demo-notebook", "Notebook Pro 14 i5", 3499.00m, null, 0m, "NOTE200", 200.00m, 150.00m),
        new("demo-phone", "Smartphone X 128GB", 1899.00m, null, 12.50m, null, 0m, 95.00m)
    ];

    public Task<PriceFetchResult> FetchAsync(PriceFetchRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ProductUrl.Contains("fail", StringComparison.OrdinalIgnoreCase))
        {
            var failRaw = JsonSerializer.Serialize(new { error = "simulated_source_failure", url = request.ProductUrl });
            return Task.FromResult(PriceFetchResult.Failed(nameof(MockCatalogSource), failRaw, "Simulated source failure (no price invented)."));
        }

        var key = ExtractKey(request.ProductUrl, request.ModelName);
        var offer = Catalog.FirstOrDefault(c => c.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                    ?? Catalog[Math.Abs(key.GetHashCode()) % Catalog.Count];

        // Slight deterministic drift by day so history charts look alive in demo mode.
        var drift = request.DemoMode
            ? (decimal)(Math.Sin(DateTime.UtcNow.DayOfYear + offer.Key.Length) * 15.0)
            : 0m;
        var listed = decimal.Round(offer.ListedPrice + drift, 2, MidpointRounding.AwayFromZero);
        if (listed < 1m) listed = offer.ListedPrice;

        var voltageMatches = VoltageMatcher.Matches(request.VoltageFilter, offer.Voltage);
        var raw = JsonSerializer.Serialize(new
        {
            source = nameof(MockCatalogSource),
            key = offer.Key,
            title = offer.Title,
            listed,
            voltage = offer.Voltage,
            shipping = offer.Shipping,
            coupon = offer.CouponCode,
            couponDiscount = offer.CouponDiscount,
            cashback = offer.Cashback,
            demo = request.DemoMode
        });

        return Task.FromResult(new PriceFetchResult(
            Success: true,
            SourceName: nameof(MockCatalogSource),
            RawPayload: raw,
            ProductTitle: offer.Title,
            ListedPrice: listed,
            Currency: "BRL",
            VoltageDetected: offer.Voltage,
            VoltageMatches: voltageMatches,
            ShippingEstimate: offer.Shipping,
            CouponCode: offer.CouponCode,
            CouponDiscount: offer.CouponDiscount,
            CashbackEstimate: offer.Cashback,
            ErrorMessage: null));
    }

    private static string ExtractKey(string url, string? model)
    {
        if (!string.IsNullOrWhiteSpace(model))
        {
            var m = model.Trim().ToLowerInvariant().Replace(' ', '-');
            if (Catalog.Any(c => c.Key.Contains(m, StringComparison.OrdinalIgnoreCase)))
                return Catalog.First(c => c.Key.Contains(m, StringComparison.OrdinalIgnoreCase)).Key;
        }

        foreach (var item in Catalog)
        {
            if (url.Contains(item.Key, StringComparison.OrdinalIgnoreCase))
                return item.Key;
        }

        return "demo-phone";
    }

    private sealed record DemoOffer(
        string Key,
        string Title,
        decimal ListedPrice,
        string? Voltage,
        decimal Shipping,
        string? CouponCode,
        decimal CouponDiscount,
        decimal Cashback);
}
