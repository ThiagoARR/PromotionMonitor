using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PromotionMonitor.Application.Abstractions;
using PromotionMonitor.Application.DTOs;
using PromotionMonitor.Domain.Entities;

namespace PromotionMonitor.Api.Endpoints;

public static class Phase2Endpoints
{
    public static RouteGroupBuilder MapPhase2Endpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api").WithTags("Phase2").RequireAuthorization();

        // --- Coupons ---
        group.MapGet("/coupons", async (ClaimsPrincipal principal, IAppDbContext db, CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            if (userId is null) return Results.Unauthorized();
            var rows = await db.CouponRules.AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CouponRuleDto(
                    c.Id, c.Code, c.Description, c.Kind, c.Value, c.MinSubtotal, c.MaxDiscount,
                    c.IsConfirmed, c.IsActive, c.ValidFrom, c.ValidUntil))
                .ToListAsync(ct);
            return Results.Ok(rows);
        });

        group.MapPost("/coupons", async (
            [FromBody] UpsertCouponRequest req, ClaimsPrincipal principal, IAppDbContext db, CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            if (userId is null) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(req.Code) || req.Value < 0)
                return Results.BadRequest(new { error = "Code required and Value must be >= 0." });

            var entity = new CouponRule
            {
                UserId = userId.Value,
                Code = req.Code.Trim().ToUpperInvariant(),
                Description = req.Description?.Trim(),
                Kind = req.Kind,
                Value = decimal.Round(req.Value, 2, MidpointRounding.AwayFromZero),
                MinSubtotal = req.MinSubtotal,
                MaxDiscount = req.MaxDiscount,
                IsConfirmed = req.IsConfirmed,
                IsActive = req.IsActive,
                ValidFrom = req.ValidFrom,
                ValidUntil = req.ValidUntil
            };
            db.CouponRules.Add(entity);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/coupons/{entity.Id}", new { entity.Id });
        });

        group.MapDelete("/coupons/{id:guid}", async (
            Guid id, ClaimsPrincipal principal, IAppDbContext db, CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            if (userId is null) return Results.Unauthorized();
            var entity = await db.CouponRules.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);
            if (entity is null) return Results.NotFound();
            db.CouponRules.Remove(entity);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        // --- Cashback ---
        group.MapGet("/cashback-rules", async (ClaimsPrincipal principal, IAppDbContext db, CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            if (userId is null) return Results.Unauthorized();
            var rows = await db.CashbackRules.AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CashbackRuleDto(c.Id, c.Name, c.Provider, c.Kind, c.Value, c.MaxCashback, c.IsActive))
                .ToListAsync(ct);
            return Results.Ok(rows);
        });

        group.MapPost("/cashback-rules", async (
            [FromBody] UpsertCashbackRequest req, ClaimsPrincipal principal, IAppDbContext db, CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            if (userId is null) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(req.Name) || req.Value < 0)
                return Results.BadRequest(new { error = "Name required and Value must be >= 0." });

            var entity = new CashbackRule
            {
                UserId = userId.Value,
                Name = req.Name.Trim(),
                Provider = req.Provider?.Trim(),
                Kind = req.Kind,
                Value = decimal.Round(req.Value, 2, MidpointRounding.AwayFromZero),
                MaxCashback = req.MaxCashback,
                IsActive = req.IsActive
            };
            db.CashbackRules.Add(entity);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/cashback-rules/{entity.Id}", new { entity.Id });
        });

        group.MapDelete("/cashback-rules/{id:guid}", async (
            Guid id, ClaimsPrincipal principal, IAppDbContext db, CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            if (userId is null) return Results.Unauthorized();
            var entity = await db.CashbackRules.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);
            if (entity is null) return Results.NotFound();
            db.CashbackRules.Remove(entity);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        // --- Shipping quote ---
        group.MapPost("/shipping/quote", async (
            [FromBody] ShippingQuoteApiRequest req,
            IShippingQuoteProvider shipping,
            CancellationToken ct) =>
        {
            if (req.ListedPrice < 0)
                return Results.BadRequest(new { error = "ListedPrice must be >= 0." });

            var result = await shipping.QuoteAsync(
                new ShippingQuoteRequest(req.Cep, req.ListedPrice, null, req.WeightKg), ct);

            return Results.Ok(new ShippingQuoteDto(
                result.Success, result.Amount, result.Provider, result.Simulated, result.ErrorMessage));
        });

        // --- Store links ---
        group.MapGet("/monitorings/{id:guid}/stores", async (
            Guid id, ClaimsPrincipal principal, IAppDbContext db, CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            if (userId is null) return Results.Unauthorized();
            var owns = await db.Monitorings.AnyAsync(m => m.Id == id && m.UserId == userId, ct);
            if (!owns) return Results.NotFound();

            var links = await db.StoreLinks.AsNoTracking()
                .Where(l => l.MonitoringId == id)
                .OrderByDescending(l => l.IsPrimary)
                .Select(l => new StoreLinkDto(l.Id, l.Label, l.ProductUrl, l.SourceKind, l.IsPrimary))
                .ToListAsync(ct);
            return Results.Ok(links);
        });

        group.MapPost("/monitorings/{id:guid}/stores", async (
            Guid id,
            [FromBody] CreateStoreLinkRequest req,
            ClaimsPrincipal principal,
            IAppDbContext db,
            IUrlSafetyValidator urlValidator,
            CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            if (userId is null) return Results.Unauthorized();
            var monitoring = await db.Monitorings
                .Include(m => m.StoreLinks)
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId, ct);
            if (monitoring is null) return Results.NotFound();

            var safety = urlValidator.Validate(req.ProductUrl);
            if (!safety.IsValid)
                return Results.BadRequest(new { error = safety.Error });
            if (string.IsNullOrWhiteSpace(req.Label))
                return Results.BadRequest(new { error = "Label is required." });

            if (req.IsPrimary)
            {
                foreach (var existing in monitoring.StoreLinks)
                    existing.IsPrimary = false;
            }

            var link = new MonitoringStoreLink
            {
                MonitoringId = id,
                Label = req.Label.Trim(),
                ProductUrl = req.ProductUrl.Trim(),
                SourceKind = req.SourceKind,
                IsPrimary = req.IsPrimary || monitoring.StoreLinks.Count == 0
            };
            db.StoreLinks.Add(link);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/monitorings/{id}/stores/{link.Id}", new { link.Id });
        });

        group.MapDelete("/monitorings/{monitoringId:guid}/stores/{linkId:guid}", async (
            Guid monitoringId, Guid linkId, ClaimsPrincipal principal, IAppDbContext db, CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            if (userId is null) return Results.Unauthorized();
            var owns = await db.Monitorings.AnyAsync(m => m.Id == monitoringId && m.UserId == userId, ct);
            if (!owns) return Results.NotFound();
            var link = await db.StoreLinks.FirstOrDefaultAsync(l => l.Id == linkId && l.MonitoringId == monitoringId, ct);
            if (link is null) return Results.NotFound();
            if (link.IsPrimary)
                return Results.BadRequest(new { error = "Cannot delete the primary store link." });
            db.StoreLinks.Remove(link);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        // --- Comparison ---
        group.MapGet("/monitorings/{id:guid}/comparison", async (
            Guid id, ClaimsPrincipal principal, IAppDbContext db, CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            if (userId is null) return Results.Unauthorized();
            var owns = await db.Monitorings.AnyAsync(m => m.Id == id && m.UserId == userId, ct);
            if (!owns) return Results.NotFound();

            var latestRound = await db.StoreOffers.AsNoTracking()
                .Where(o => o.MonitoringId == id)
                .OrderByDescending(o => o.CollectedAt)
                .Select(o => o.ComparisonRoundId)
                .FirstOrDefaultAsync(ct);

            if (latestRound == Guid.Empty)
                return Results.Ok(Array.Empty<StoreOfferDto>());

            var offers = await db.StoreOffers.AsNoTracking()
                .Where(o => o.MonitoringId == id && o.ComparisonRoundId == latestRound)
                .OrderBy(o => o.PayAtCheckout ?? decimal.MaxValue)
                .Select(o => new StoreOfferDto(
                    o.Id, o.ComparisonRoundId, o.StoreLabel, o.ProductUrl, o.SourceKind, o.CollectedAt,
                    o.Success, o.ErrorMessage, o.ProductTitle, o.ListedPrice, o.Shipping, o.ShippingProvider,
                    o.ShippingSimulated, o.CouponCode, o.CouponDiscount, o.CouponConfirmed, o.CashbackEstimate,
                    o.PayAtCheckout, o.NetAfterCashback, o.IsBestPayAtCheckout, o.IsBestNetAfterCashback))
                .ToListAsync(ct);

            return Results.Ok(offers);
        });

        return group;
    }
}
