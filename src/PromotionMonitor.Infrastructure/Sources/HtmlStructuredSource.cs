using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using PromotionMonitor.Application.Abstractions;
using PromotionMonitor.Application.Services;
using PromotionMonitor.Domain.Enums;

namespace PromotionMonitor.Infrastructure.Sources;

/// <summary>
/// Fetches product pages and extracts JSON-LD Product / OpenGraph price when present.
/// In DemoMode, returns a deterministic structured fixture without network I/O.
/// Does not bypass CAPTCHA/antibot — failures are recorded without inventing prices.
/// </summary>
public sealed class HtmlStructuredSource : IPriceSource
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IUrlSafetyValidator _urlValidator;
    private readonly ILogger<HtmlStructuredSource> _logger;

    public HtmlStructuredSource(
        IHttpClientFactory httpClientFactory,
        IUrlSafetyValidator urlValidator,
        ILogger<HtmlStructuredSource> logger)
    {
        _httpClientFactory = httpClientFactory;
        _urlValidator = urlValidator;
        _logger = logger;
    }

    public SourceKind Kind => SourceKind.HtmlStructured;

    public async Task<PriceFetchResult> FetchAsync(PriceFetchRequest request, CancellationToken cancellationToken = default)
    {
        if (request.DemoMode)
            return BuildDemoResult(request);

        var safety = _urlValidator.Validate(request.ProductUrl);
        if (!safety.IsValid)
            return PriceFetchResult.Failed(nameof(HtmlStructuredSource), "{}", safety.Error ?? "Unsafe URL");

        try
        {
            var client = _httpClientFactory.CreateClient("PriceFetch");
            using var response = await client.GetAsync(request.ProductUrl, cancellationToken);
            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var raw = JsonSerializer.Serialize(new
            {
                status = (int)response.StatusCode,
                url = request.ProductUrl,
                length = html.Length,
                snippet = html.Length > 500 ? html[..500] : html
            });

            if (!response.IsSuccessStatusCode)
            {
                return PriceFetchResult.Failed(
                    nameof(HtmlStructuredSource),
                    raw,
                    $"HTTP {(int)response.StatusCode}; no price invented.");
            }

            var parsed = TryParseHtml(html, request.VoltageFilter);
            if (parsed is null)
            {
                return PriceFetchResult.Failed(
                    nameof(HtmlStructuredSource),
                    raw,
                    "No JSON-LD/OpenGraph product price found; no price invented.");
            }

            return parsed with { RawPayload = raw, SourceName = nameof(HtmlStructuredSource) };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HtmlStructuredSource failed for {Url}", request.ProductUrl);
            return PriceFetchResult.Failed(nameof(HtmlStructuredSource), "{}", ex.Message);
        }
    }

    private static PriceFetchResult BuildDemoResult(PriceFetchRequest request)
    {
        var title = request.ModelName is { Length: > 0 }
            ? $"{request.ModelName} (structured demo)"
            : "Structured Demo Product 110V";
        var voltage = title.Contains("220") ? "220V" : title.Contains("Bivolt", StringComparison.OrdinalIgnoreCase) ? "Bivolt" : "110V";
        var listed = 599.90m;
        var shipping = string.IsNullOrWhiteSpace(request.Cep) ? 29.90m : 19.90m;
        var raw = JsonSerializer.Serialize(new
        {
            mode = "demo",
            jsonLd = new
            {
                @type = "Product",
                name = title,
                offers = new { @type = "Offer", price = listed, priceCurrency = "BRL" }
            }
        });

        return new PriceFetchResult(
            true,
            nameof(HtmlStructuredSource),
            raw,
            title,
            listed,
            "BRL",
            voltage,
            VoltageMatcher.Matches(request.VoltageFilter, voltage),
            shipping,
            "DEMO5",
            30.00m,
            25.00m,
            null);
    }

    private static PriceFetchResult? TryParseHtml(string html, Voltage filter)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // JSON-LD
        var scripts = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
        if (scripts is not null)
        {
            foreach (var script in scripts)
            {
                try
                {
                    using var docJson = JsonDocument.Parse(script.InnerText);
                    if (TryReadProduct(docJson.RootElement, out var title, out var price, out var currency))
                    {
                        var voltage = DetectVoltage(html + " " + title);
                        return new PriceFetchResult(
                            true, nameof(HtmlStructuredSource), script.InnerText, title, price, currency ?? "BRL",
                            voltage, VoltageMatcher.Matches(filter, voltage), null, null, null, null, null);
                    }
                }
                catch (JsonException)
                {
                    // continue
                }
            }
        }

        // OpenGraph
        var ogTitle = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", null);
        var ogPrice = doc.DocumentNode.SelectSingleNode("//meta[@property='product:price:amount']")?.GetAttributeValue("content", null)
                      ?? doc.DocumentNode.SelectSingleNode("//meta[@property='og:price:amount']")?.GetAttributeValue("content", null);
        var ogCurrency = doc.DocumentNode.SelectSingleNode("//meta[@property='product:price:currency']")?.GetAttributeValue("content", null) ?? "BRL";

        if (ogPrice is not null &&
            decimal.TryParse(ogPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedPrice))
        {
            var voltage = DetectVoltage(html + " " + (ogTitle ?? string.Empty));
            return new PriceFetchResult(
                true, nameof(HtmlStructuredSource), html.Length > 2000 ? html[..2000] : html,
                ogTitle, parsedPrice, ogCurrency, voltage, VoltageMatcher.Matches(filter, voltage),
                null, null, null, null, null);
        }

        return null;
    }

    private static bool TryReadProduct(JsonElement el, out string? title, out decimal price, out string? currency)
    {
        title = null;
        price = 0;
        currency = "BRL";

        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in el.EnumerateArray())
            {
                if (TryReadProduct(item, out title, out price, out currency))
                    return true;
            }
            return false;
        }

        if (el.ValueKind != JsonValueKind.Object)
            return false;

        if (el.TryGetProperty("@graph", out var graph))
            return TryReadProduct(graph, out title, out price, out currency);

        var type = el.TryGetProperty("@type", out var t) ? t.ToString() : null;
        if (type is not null && type.Contains("Product", StringComparison.OrdinalIgnoreCase))
        {
            if (el.TryGetProperty("name", out var name))
                title = name.GetString();

            if (el.TryGetProperty("offers", out var offers))
            {
                if (offers.ValueKind == JsonValueKind.Array && offers.GetArrayLength() > 0)
                    offers = offers[0];

                if (offers.TryGetProperty("price", out var p))
                {
                    if (p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out price))
                    {
                        if (offers.TryGetProperty("priceCurrency", out var c))
                            currency = c.GetString();
                        return true;
                    }

                    if (p.ValueKind == JsonValueKind.String &&
                        decimal.TryParse(p.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out price))
                    {
                        if (offers.TryGetProperty("priceCurrency", out var c))
                            currency = c.GetString();
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static string? DetectVoltage(string text)
    {
        var m = Regex.Match(text, @"\b(bivolt|110\s*/\s*220|127\s*/\s*220|220\s*V|110\s*V|127\s*V)\b", RegexOptions.IgnoreCase);
        return m.Success ? m.Value.Trim() : null;
    }
}
