using PromotionMonitor.Domain.Enums;

namespace PromotionMonitor.Application.Abstractions;

public sealed record PriceFetchRequest(
    Guid MonitoringId,
    string ProductUrl,
    string? ModelName,
    Voltage VoltageFilter,
    string? Cep,
    bool DemoMode);

public sealed record PriceFetchResult(
    bool Success,
    string SourceName,
    string RawPayload,
    string? ProductTitle,
    decimal? ListedPrice,
    string Currency,
    string? VoltageDetected,
    bool? VoltageMatches,
    decimal? ShippingEstimate,
    string? CouponCode,
    decimal? CouponDiscount,
    decimal? CashbackEstimate,
    string? ErrorMessage)
{
    public static PriceFetchResult Failed(string sourceName, string rawPayload, string error) =>
        new(false, sourceName, rawPayload, null, null, "BRL", null, null, null, null, null, null, error);
}

public interface IPriceSource
{
    SourceKind Kind { get; }
    Task<PriceFetchResult> FetchAsync(PriceFetchRequest request, CancellationToken cancellationToken = default);
}
