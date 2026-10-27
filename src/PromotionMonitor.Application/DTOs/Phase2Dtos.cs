using PromotionMonitor.Domain.Enums;

namespace PromotionMonitor.Application.DTOs;

public sealed record CouponRuleDto(
    Guid Id,
    string Code,
    string? Description,
    DiscountKind Kind,
    decimal Value,
    decimal? MinSubtotal,
    decimal? MaxDiscount,
    bool IsConfirmed,
    bool IsActive,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil);

public sealed record UpsertCouponRequest(
    string Code,
    string? Description,
    DiscountKind Kind,
    decimal Value,
    decimal? MinSubtotal,
    decimal? MaxDiscount,
    bool IsConfirmed,
    bool IsActive,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil);

public sealed record CashbackRuleDto(
    Guid Id,
    string Name,
    string? Provider,
    DiscountKind Kind,
    decimal Value,
    decimal? MaxCashback,
    bool IsActive);

public sealed record UpsertCashbackRequest(
    string Name,
    string? Provider,
    DiscountKind Kind,
    decimal Value,
    decimal? MaxCashback,
    bool IsActive);

public sealed record StoreLinkDto(
    Guid Id,
    string Label,
    string ProductUrl,
    SourceKind SourceKind,
    bool IsPrimary);

public sealed record CreateStoreLinkRequest(
    string Label,
    string ProductUrl,
    SourceKind SourceKind,
    bool IsPrimary);

public sealed record StoreOfferDto(
    Guid Id,
    Guid ComparisonRoundId,
    string StoreLabel,
    string ProductUrl,
    SourceKind SourceKind,
    DateTimeOffset CollectedAt,
    bool Success,
    string? ErrorMessage,
    string? ProductTitle,
    decimal? ListedPrice,
    decimal? Shipping,
    string? ShippingProvider,
    bool ShippingSimulated,
    string? CouponCode,
    decimal? CouponDiscount,
    bool CouponConfirmed,
    decimal? CashbackEstimate,
    decimal? PayAtCheckout,
    decimal? NetAfterCashback,
    bool IsBestPayAtCheckout,
    bool IsBestNetAfterCashback);

public sealed record ShippingQuoteDto(
    bool Success,
    decimal? Amount,
    string Provider,
    bool Simulated,
    string? ErrorMessage);

public sealed record ShippingQuoteApiRequest(string Cep, decimal ListedPrice, decimal WeightKg = 1.0m);
