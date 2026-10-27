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
    private readonly ICouponEvaluator _coupons;
    private readonly ICashbackEvaluator _cashback;
    private readonly IShippingQuoteProvider _shipping;
    private readonly IAlertDispatcher _alerts;
    private readonly ILogger<MonitoringCollector> _logger;

    public MonitoringCollector(
        IAppDbContext db,
        IEnumerable<IPriceSource> sources,
        IEffectiveCostCalculator calculator,
        ICouponEvaluator coupons,
        ICashbackEvaluator cashback,
        IShippingQuoteProvider shipping,
        IAlertDispatcher alerts,
        ILogger<MonitoringCollector> logger)
    {
        _db = db;
        _sources = sources;
        _calculator = calculator;
        _coupons = coupons;
        _cashback = cashback;
        _shipping = shipping;
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
            .Include(m => m.StoreLinks)
            .FirstOrDefaultAsync(m => m.Id == monitoringId, cancellationToken)
            ?? throw new InvalidOperationException($"Monitoring {monitoringId} not found.");

        await EnsurePrimaryStoreLinkAsync(monitoring, cancellationToken);

        // Re-read links after possible insert to avoid InMemory concurrency glitches.
        var links = await _db.StoreLinks
            .Where(l => l.MonitoringId == monitoring.Id)
            .OrderByDescending(l => l.IsPrimary)
            .ThenBy(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        var roundId = Guid.NewGuid();
        var offers = new List<StoreOffer>();

        foreach (var link in links)
        {
            var offer = await CollectStoreAsync(monitoring, link, roundId, cancellationToken);
            offers.Add(offer);
            _db.StoreOffers.Add(offer);
        }

        MarkBestOffers(offers);

        // Primary observation (Phase 1 compatible) from primary link.
        var primaryLinkId = links.FirstOrDefault(l => l.IsPrimary)?.Id ?? links.First().Id;
        var primary = offers.FirstOrDefault(o => o.StoreLinkId == primaryLinkId) ?? offers.First();
        var primaryResult = ToFetchResult(primary);
        var fingerprint = ComputeFingerprint(monitoring.Id, primaryResult);
        var isDuplicate = await _db.Observations.AnyAsync(
            o => o.MonitoringId == monitoring.Id && o.Fingerprint == fingerprint && o.Success,
            cancellationToken);

        var observation = new PriceObservation
        {
            MonitoringId = monitoring.Id,
            ObservedAt = DateTimeOffset.UtcNow,
            SourceName = primary.SourceName,
            RawPayload = primary.RawPayload,
            NormalizedJson = JsonSerializer.Serialize(primaryResult),
            ProductTitle = primary.ProductTitle,
            ListedPrice = primary.Success ? primary.ListedPrice : null,
            Currency = "BRL",
            VoltageDetected = primary.VoltageDetected,
            VoltageMatches = primary.VoltageMatches,
            ShippingEstimate = primary.Success ? primary.Shipping : null,
            CouponCode = primary.Success ? primary.CouponCode : null,
            CouponDiscount = primary.Success ? primary.CouponDiscount : null,
            CashbackEstimate = primary.Success ? primary.CashbackEstimate : null,
            Success = primary.Success,
            ErrorMessage = primary.ErrorMessage,
            Fingerprint = fingerprint
        };

        if (primary.Success && primary.ListedPrice is decimal && primary.PayAtCheckout is decimal)
        {
            observation.Calculation = new PriceCalculation
            {
                ObservationId = observation.Id,
                ListedPrice = primary.ListedPrice ?? 0m,
                Shipping = primary.Shipping ?? 0m,
                ShippingProvider = primary.ShippingProvider,
                ShippingSimulated = primary.ShippingSimulated,
                CouponDiscount = primary.CouponDiscount ?? 0m,
                CouponCode = primary.CouponCode,
                CouponConfirmed = primary.CouponConfirmed,
                PayAtCheckout = primary.PayAtCheckout.Value,
                CashbackEstimate = primary.CashbackEstimate ?? 0m,
                CashbackProgram = null,
                NetAfterCashback = primary.NetAfterCashback ?? primary.PayAtCheckout.Value,
                Notes = BuildNotes(primary),
                CalculatedAt = DateTimeOffset.UtcNow
            };
        }

        _db.Observations.Add(observation);
        monitoring.LastCheckedAt = DateTimeOffset.UtcNow;
        monitoring.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        if (primary.Success &&
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

    private async Task<StoreOffer> CollectStoreAsync(
        Monitoring monitoring,
        MonitoringStoreLink link,
        Guid roundId,
        CancellationToken cancellationToken)
    {
        var source = _sources.FirstOrDefault(s => s.Kind == link.SourceKind);
        if (source is null)
        {
            return FailedOffer(monitoring, link, roundId, "none", "{}", $"No source for {link.SourceKind}");
        }

        var request = new PriceFetchRequest(
            monitoring.Id,
            link.ProductUrl,
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
            _logger.LogWarning(ex, "Source {Source} threw for link {LinkId}", source.Kind, link.Id);
            result = PriceFetchResult.Failed(source.Kind.ToString(), "{}", ex.Message);
        }

        if (!result.Success || result.ListedPrice is not decimal listed)
        {
            return FailedOffer(monitoring, link, roundId, result.SourceName, result.RawPayload,
                result.ErrorMessage ?? "Fetch failed");
        }

        // Shipping by CEP — never invent on failure.
        decimal shipping = 0m;
        string? shippingProvider = null;
        var shippingSimulated = false;
        if (!string.IsNullOrWhiteSpace(monitoring.Cep))
        {
            var quote = await _shipping.QuoteAsync(
                new ShippingQuoteRequest(monitoring.Cep!, listed, monitoring.Id),
                cancellationToken);

            _db.ShippingQuotes.Add(new ShippingQuoteRecord
            {
                MonitoringId = monitoring.Id,
                Cep = SimulatedCepDigits(monitoring.Cep) ?? monitoring.Cep!,
                ListedPrice = listed,
                QuoteAmount = quote.Amount,
                Provider = quote.Provider,
                Simulated = quote.Simulated,
                Success = quote.Success,
                ErrorMessage = quote.ErrorMessage,
                RawPayload = quote.RawPayload,
                QuotedAt = DateTimeOffset.UtcNow
            });

            if (quote.Success && quote.Amount is decimal amount)
            {
                shipping = amount;
                shippingProvider = quote.Provider;
                shippingSimulated = quote.Simulated;
            }
            else
            {
                // Fall back to source shipping estimate only if present; else 0 with note via provider failure.
                shipping = result.ShippingEstimate ?? 0m;
                shippingProvider = quote.Provider + "-fallback-source";
                shippingSimulated = true;
            }
        }
        else
        {
            shipping = result.ShippingEstimate ?? 0m;
            shippingProvider = "source-estimate";
            shippingSimulated = true;
        }

        var couponEval = await _coupons.EvaluateBestAsync(
            monitoring.UserId, listed, shipping, result.CouponCode, result.CouponDiscount, cancellationToken);

        var calc = _calculator.Calculate(new CostInputs(
            ListedPrice: listed,
            Shipping: shipping,
            CouponDiscount: couponEval.Discount,
            CashbackEstimate: 0m, // filled after pay-at-checkout basis
            CouponConfirmed: couponEval.Confirmed,
            CouponCode: couponEval.Code,
            ShippingProvider: shippingProvider,
            ShippingSimulated: shippingSimulated));

        var cashbackEval = await _cashback.EvaluateBestAsync(
            monitoring.UserId,
            calc.PayAtCheckout,
            result.CashbackEstimate,
            cancellationToken);

        calc = _calculator.Calculate(new CostInputs(
            ListedPrice: listed,
            Shipping: shipping,
            CouponDiscount: couponEval.Discount,
            CashbackEstimate: cashbackEval.Estimate,
            CouponConfirmed: couponEval.Confirmed,
            CouponCode: couponEval.Code,
            ShippingProvider: shippingProvider,
            ShippingSimulated: shippingSimulated,
            CashbackProgram: cashbackEval.ProgramName));

        return new StoreOffer
        {
            MonitoringId = monitoring.Id,
            ComparisonRoundId = roundId,
            StoreLinkId = link.Id,
            StoreLabel = link.Label,
            ProductUrl = link.ProductUrl,
            SourceKind = link.SourceKind,
            SourceName = result.SourceName,
            CollectedAt = DateTimeOffset.UtcNow,
            Success = true,
            ProductTitle = result.ProductTitle,
            ListedPrice = listed,
            Shipping = shipping,
            ShippingProvider = shippingProvider,
            ShippingSimulated = shippingSimulated,
            CouponCode = couponEval.Code,
            CouponDiscount = calc.CouponDiscount,
            CouponConfirmed = calc.CouponConfirmed,
            CashbackEstimate = calc.CashbackEstimate,
            PayAtCheckout = calc.PayAtCheckout,
            NetAfterCashback = calc.NetAfterCashback,
            VoltageDetected = result.VoltageDetected,
            VoltageMatches = result.VoltageMatches,
            RawPayload = result.RawPayload
        };
    }

    private async Task EnsurePrimaryStoreLinkAsync(Monitoring monitoring, CancellationToken cancellationToken)
    {
        var existing = await _db.StoreLinks
            .Where(l => l.MonitoringId == monitoring.Id)
            .ToListAsync(cancellationToken);

        if (existing.Count == 0)
        {
            _db.StoreLinks.Add(new MonitoringStoreLink
            {
                MonitoringId = monitoring.Id,
                Label = "Primary",
                ProductUrl = monitoring.ProductUrl,
                SourceKind = monitoring.SourceKind,
                IsPrimary = true
            });
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (!existing.Any(l => l.IsPrimary))
        {
            existing.OrderBy(l => l.CreatedAt).First().IsPrimary = true;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private static void MarkBestOffers(List<StoreOffer> offers)
    {
        var ok = offers.Where(o => o.Success && o.PayAtCheckout is not null).ToList();
        if (ok.Count == 0) return;

        var bestPay = ok.Min(o => o.PayAtCheckout!.Value);
        foreach (var o in ok.Where(o => o.PayAtCheckout == bestPay))
            o.IsBestPayAtCheckout = true;

        var withNet = ok.Where(o => o.NetAfterCashback is not null).ToList();
        if (withNet.Count == 0) return;
        var bestNet = withNet.Min(o => o.NetAfterCashback!.Value);
        foreach (var o in withNet.Where(o => o.NetAfterCashback == bestNet))
            o.IsBestNetAfterCashback = true;
    }

    private static StoreOffer FailedOffer(
        Monitoring monitoring, MonitoringStoreLink link, Guid roundId,
        string sourceName, string raw, string error) =>
        new()
        {
            MonitoringId = monitoring.Id,
            ComparisonRoundId = roundId,
            StoreLinkId = link.Id,
            StoreLabel = link.Label,
            ProductUrl = link.ProductUrl,
            SourceKind = link.SourceKind,
            SourceName = sourceName,
            CollectedAt = DateTimeOffset.UtcNow,
            Success = false,
            ErrorMessage = error,
            RawPayload = raw
        };

    private static PriceFetchResult ToFetchResult(StoreOffer offer) =>
        offer.Success
            ? new PriceFetchResult(
                true, offer.SourceName, offer.RawPayload, offer.ProductTitle, offer.ListedPrice, "BRL",
                offer.VoltageDetected, offer.VoltageMatches, offer.Shipping, offer.CouponCode,
                offer.CouponDiscount, offer.CashbackEstimate, null)
            : PriceFetchResult.Failed(offer.SourceName, offer.RawPayload, offer.ErrorMessage ?? "failed");

    private static string BuildNotes(StoreOffer primary)
    {
        var notes = primary.CouponConfirmed
            ? "Coupon applied to checkout. Cashback is estimated and never reduces PayAtCheckout."
            : "Coupon not confirmed; ignored at checkout. Cashback is estimated and never reduces PayAtCheckout.";
        if (primary.ShippingSimulated)
            notes += " Shipping quote is simulated.";
        return notes;
    }

    private static string? SimulatedCepDigits(string? cep)
    {
        if (string.IsNullOrWhiteSpace(cep)) return null;
        var digits = new string(cep.Where(char.IsDigit).ToArray());
        return digits.Length == 8 ? digits : null;
    }

    private static string ComputeFingerprint(Guid monitoringId, PriceFetchResult result)
    {
        var material =
            $"{monitoringId}|{result.ListedPrice}|{result.ShippingEstimate}|{result.CouponCode}|{result.CouponDiscount}|{result.CashbackEstimate}|{result.VoltageDetected}|{result.ProductTitle}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash);
    }
}
