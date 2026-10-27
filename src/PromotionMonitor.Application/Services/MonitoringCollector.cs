using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PromotionMonitor.Application.Abstractions;
using PromotionMonitor.Domain.Entities;
using PromotionMonitor.Domain.Enums;

namespace PromotionMonitor.Application.Services;

public interface IMonitoringCollector
{
    Task CollectAsync(Guid monitoringId, CancellationToken cancellationToken = default);
    Task CollectDueAsync(CancellationToken cancellationToken = default);
}

public sealed class MonitoringCollector : IMonitoringCollector
{
    private readonly IAppDbContext _db;
    private readonly IEnumerable<IPriceSource> _sources;
    private readonly IEffectiveCostCalculator _calculator;
    private readonly IAlertDispatcher _alerts;
    private readonly ILogger<MonitoringCollector> _logger;

    public MonitoringCollector(
        IAppDbContext db,
        IEnumerable<IPriceSource> sources,
        IEffectiveCostCalculator calculator,
        IAlertDispatcher alerts,
        ILogger<MonitoringCollector> logger)
    {
        _db = db;
        _sources = sources;
        _calculator = calculator;
        _alerts = alerts;
        _logger = logger;
    }

    public async Task CollectDueAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var active = await _db.Monitorings
            .Where(m => m.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var monitoring in active)
        {
            var due = monitoring.LastCheckedAt is null ||
                      monitoring.LastCheckedAt.Value.AddMinutes(monitoring.ScheduleMinutes) <= now;
            if (!due)
                continue;

            try
            {
                await CollectAsync(monitoring.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed collecting monitoring {MonitoringId}", monitoring.Id);
            }
        }
    }

    public async Task CollectAsync(Guid monitoringId, CancellationToken cancellationToken = default)
    {
        var monitoring = await _db.Monitorings
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == monitoringId, cancellationToken)
            ?? throw new InvalidOperationException($"Monitoring {monitoringId} not found.");

        var source = _sources.FirstOrDefault(s => s.Kind == monitoring.SourceKind)
            ?? throw new InvalidOperationException($"No source registered for {monitoring.SourceKind}.");

        var request = new PriceFetchRequest(
            monitoring.Id,
            monitoring.ProductUrl,
            monitoring.ModelName,
            monitoring.VoltageFilter,
            monitoring.Cep,
            monitoring.DemoMode);

        PriceFetchResult result;
        try
        {
            result = await source.FetchAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Source {Source} threw for monitoring {Id}", source.Kind, monitoring.Id);
            result = PriceFetchResult.Failed(source.Kind.ToString(), "{}", ex.Message);
        }

        // Never invent prices on failure — store failure observation only.
        var fingerprint = ComputeFingerprint(monitoring.Id, result);
        var isDuplicate = await _db.Observations.AnyAsync(
            o => o.MonitoringId == monitoring.Id && o.Fingerprint == fingerprint && o.Success,
            cancellationToken);

        var observation = new PriceObservation
        {
            MonitoringId = monitoring.Id,
            ObservedAt = DateTimeOffset.UtcNow,
            SourceName = result.SourceName,
            RawPayload = result.RawPayload,
            NormalizedJson = JsonSerializer.Serialize(result),
            ProductTitle = result.ProductTitle,
            ListedPrice = result.Success ? result.ListedPrice : null,
            Currency = result.Currency,
            VoltageDetected = result.VoltageDetected,
            VoltageMatches = result.VoltageMatches,
            ShippingEstimate = result.Success ? result.ShippingEstimate : null,
            CouponCode = result.Success ? result.CouponCode : null,
            CouponDiscount = result.Success ? result.CouponDiscount : null,
            CashbackEstimate = result.Success ? result.CashbackEstimate : null,
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            Fingerprint = fingerprint
        };

        if (result.Success && result.ListedPrice is decimal listed)
        {
            var calc = _calculator.Calculate(new CostInputs(
                ListedPrice: listed,
                Shipping: result.ShippingEstimate ?? 0m,
                CouponDiscount: result.CouponDiscount ?? 0m,
                CashbackEstimate: result.CashbackEstimate ?? 0m,
                CouponConfirmed: !string.IsNullOrWhiteSpace(result.CouponCode)));
            calc.ObservationId = observation.Id;
            observation.Calculation = calc;
        }

        _db.Observations.Add(observation);
        monitoring.LastCheckedAt = DateTimeOffset.UtcNow;
        monitoring.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        if (result.Success &&
            observation.Calculation is not null &&
            observation.VoltageMatches != false &&
            observation.Calculation.PayAtCheckout <= monitoring.MaxPrice)
        {
            if (isDuplicate)
            {
                _logger.LogInformation(
                    "Skipping alert for duplicate observation fingerprint on monitoring {Id}",
                    monitoring.Id);
            }
            else
            {
                await _alerts.DispatchPriceAlertAsync(monitoring, observation, cancellationToken);
            }
        }
    }

    private static string ComputeFingerprint(Guid monitoringId, PriceFetchResult result)
    {
        var material =
            $"{monitoringId}|{result.ListedPrice}|{result.ShippingEstimate}|{result.CouponCode}|{result.CouponDiscount}|{result.CashbackEstimate}|{result.VoltageDetected}|{result.ProductTitle}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash);
    }
}
