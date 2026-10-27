namespace PromotionMonitor.Application.Abstractions;

public sealed record ShippingQuoteRequest(
    string Cep,
    decimal ListedPrice,
    Guid? MonitoringId = null,
    decimal WeightKg = 1.0m);

public sealed record ShippingQuoteResult(
    bool Success,
    decimal? Amount,
    string Provider,
    bool Simulated,
    string RawPayload,
    string? ErrorMessage)
{
    public static ShippingQuoteResult Failed(string provider, string raw, string error, bool simulated = true) =>
        new(false, null, provider, simulated, raw, error);
}

public interface IShippingQuoteProvider
{
    string Name { get; }
    Task<ShippingQuoteResult> QuoteAsync(ShippingQuoteRequest request, CancellationToken cancellationToken = default);
}
