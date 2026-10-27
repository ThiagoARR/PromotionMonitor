using PromotionMonitor.Domain.Enums;

namespace PromotionMonitor.Application.DTOs;

public sealed record CreateMonitoringRequest(
    string Name,
    string ProductUrl,
    string? ModelName,
    Voltage VoltageFilter,
    decimal? TargetPrice,
    decimal MaxPrice,
    string? Cep,
    int ScheduleMinutes,
    SourceKind SourceKind,
    bool DemoMode);

public sealed record UpdateMonitoringRequest(
    string Name,
    string? ModelName,
    Voltage VoltageFilter,
    decimal? TargetPrice,
    decimal MaxPrice,
    string? Cep,
    int ScheduleMinutes,
    bool IsActive,
    bool DemoMode);

public sealed record MonitoringListItemDto(
    Guid Id,
    string Name,
    string ProductUrl,
    string? ModelName,
    Voltage VoltageFilter,
    decimal? TargetPrice,
    decimal MaxPrice,
    int ScheduleMinutes,
    SourceKind SourceKind,
    bool IsActive,
    bool DemoMode,
    DateTimeOffset? LastCheckedAt,
    decimal? LatestPayAtCheckout,
    decimal? LatestListedPrice,
    bool? LatestVoltageMatches,
    string? LatestStatus);

public sealed record PriceCalculationDto(
    decimal ListedPrice,
    decimal Shipping,
    decimal CouponDiscount,
    decimal PayAtCheckout,
    decimal CashbackEstimate,
    decimal NetAfterCashback,
    string Notes);

public sealed record ObservationDto(
    Guid Id,
    DateTimeOffset ObservedAt,
    string SourceName,
    bool Success,
    string? ProductTitle,
    decimal? ListedPrice,
    string? VoltageDetected,
    bool? VoltageMatches,
    string? ErrorMessage,
    PriceCalculationDto? Calculation);

public sealed record MonitoringDetailDto(
    Guid Id,
    string Name,
    string ProductUrl,
    string? ModelName,
    Voltage VoltageFilter,
    decimal? TargetPrice,
    decimal MaxPrice,
    string? Cep,
    int ScheduleMinutes,
    SourceKind SourceKind,
    bool IsActive,
    bool DemoMode,
    DateTimeOffset? LastCheckedAt,
    IReadOnlyList<ObservationDto> Observations,
    IReadOnlyList<NotificationDeliveryDto> RecentAlerts);

public sealed record NotificationDeliveryDto(
    Guid Id,
    NotificationChannel Channel,
    string Status,
    string Subject,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DeliveredAt,
    bool IsDuplicateSkip);

public sealed record DashboardSummaryDto(
    int ActiveMonitorings,
    int ObservationsLast24h,
    int AlertsLast24h,
    IReadOnlyList<MonitoringListItemDto> Monitorings);
