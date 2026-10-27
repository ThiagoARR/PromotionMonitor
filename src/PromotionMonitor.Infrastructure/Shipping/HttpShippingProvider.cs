using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PromotionMonitor.Application.Abstractions;

namespace PromotionMonitor.Infrastructure.Shipping;

public sealed class ShippingApiOptions
{
    public const string SectionName = "Shipping";
    /// <summary>When empty, the composite provider uses simulation only.</summary>
    public string? ApiUrl { get; set; }
    public string? ApiKey { get; set; }
}

/// <summary>
/// Optional live shipping HTTP API. Credentials via env; on missing config or failure does not invent prices.
/// </summary>
public sealed class HttpShippingProvider : IShippingQuoteProvider
{
    private readonly ShippingApiOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpShippingProvider> _logger;

    public HttpShippingProvider(
        IOptions<ShippingApiOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<HttpShippingProvider> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string Name => "HttpShippingApi";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.ApiUrl) && !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<ShippingQuoteResult> QuoteAsync(ShippingQuoteRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return ShippingQuoteResult.Failed(
                Name, "{}", "Shipping API credentials missing.", simulated: false);
        }

        var cep = SimulatedShippingProvider.NormalizeCep(request.Cep);
        if (cep is null)
        {
            return ShippingQuoteResult.Failed(
                Name, JsonSerializer.Serialize(new { error = "invalid_cep" }),
                "Invalid CEP; shipping not invented.", simulated: false);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("ShippingApi");
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.ApiUrl);
            httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_options.ApiKey}");
            httpRequest.Content = JsonContent.Create(new
            {
                cep,
                listedPrice = request.ListedPrice,
                weightKg = request.WeightKg
            });

            using var response = await client.SendAsync(httpRequest, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return ShippingQuoteResult.Failed(
                    Name, body,
                    $"Shipping API HTTP {(int)response.StatusCode}; no freight invented.",
                    simulated: false);
            }

            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            if (!doc.RootElement.TryGetProperty("amount", out var amountEl) ||
                !amountEl.TryGetDecimal(out var amount) ||
                amount < 0)
            {
                return ShippingQuoteResult.Failed(
                    Name, body, "Shipping API response missing valid amount; no freight invented.",
                    simulated: false);
            }

            return new ShippingQuoteResult(true, decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
                Name, Simulated: false, body, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HttpShippingProvider failed");
            return ShippingQuoteResult.Failed(Name, "{}", ex.Message, simulated: false);
        }
    }
}

/// <summary>Prefers live HTTP when configured; otherwise simulated CEP quotes.</summary>
public sealed class CompositeShippingProvider : IShippingQuoteProvider
{
    private readonly HttpShippingProvider _http;
    private readonly SimulatedShippingProvider _simulated;
    private readonly ILogger<CompositeShippingProvider> _logger;

    public CompositeShippingProvider(
        HttpShippingProvider http,
        SimulatedShippingProvider simulated,
        ILogger<CompositeShippingProvider> logger)
    {
        _http = http;
        _simulated = simulated;
        _logger = logger;
    }

    public string Name => "CompositeShipping";

    public async Task<ShippingQuoteResult> QuoteAsync(ShippingQuoteRequest request, CancellationToken cancellationToken = default)
    {
        if (_http.IsConfigured)
        {
            var live = await _http.QuoteAsync(request, cancellationToken);
            if (live.Success)
                return live;

            _logger.LogWarning(
                "Live shipping failed ({Error}); falling back to simulated quote",
                live.ErrorMessage);
        }

        return await _simulated.QuoteAsync(request, cancellationToken);
    }
}
