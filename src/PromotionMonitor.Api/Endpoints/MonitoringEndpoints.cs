using System.Security.Claims;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PromotionMonitor.Application.Abstractions;
using PromotionMonitor.Application.DTOs;
using PromotionMonitor.Application.Services;
using PromotionMonitor.Domain.Entities;
using PromotionMonitor.Domain.Enums;
using PromotionMonitor.Infrastructure.Jobs;

namespace PromotionMonitor.Api.Endpoints;

public static class MonitoringEndpoints
{
    public static RouteGroupBuilder MapMonitoringEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api").WithTags("Monitorings").RequireAuthorization();

        group.MapGet("/dashboard", async (ClaimsPrincipal principal, IAppDbContext db, CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var monitorings = await db.Monitorings
                .AsNoTracking()
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.UpdatedAt)
                .ToListAsync(ct);

            var ids = monitorings.Select(m => m.Id).ToList();
            var latestObs = await db.Observations
                .AsNoTracking()
                .Include(o => o.Calculation)
                .Where(o => ids.Contains(o.MonitoringId))
                .GroupBy(o => o.MonitoringId)
                .Select(g => g.OrderByDescending(o => o.ObservedAt).First())
                .ToListAsync(ct);

            var since = DateTimeOffset.UtcNow.AddHours(-24);
            var obs24 = await db.Observations.CountAsync(
                o => ids.Contains(o.MonitoringId) && o.ObservedAt >= since, ct);
            var alerts24 = await db.Deliveries.CountAsync(
                d => d.UserId == userId && d.CreatedAt >= since, ct);

            var items = monitorings.Select(m =>
            {
                var latest = latestObs.FirstOrDefault(o => o.MonitoringId == m.Id);
                return new MonitoringListItemDto(
                    m.Id, m.Name, m.ProductUrl, m.ModelName, m.VoltageFilter,
                    m.TargetPrice, m.MaxPrice, m.ScheduleMinutes, m.SourceKind,
                    m.IsActive, m.DemoMode, m.LastCheckedAt,
                    latest?.Calculation?.PayAtCheckout,
                    latest?.ListedPrice,
                    latest?.VoltageMatches,
                    latest is null ? "never" : latest.Success ? "ok" : "error");
            }).ToList();

            return Results.Ok(new DashboardSummaryDto(
                monitorings.Count(m => m.IsActive),
                obs24,
                alerts24,
                items));
        });

        group.MapGet("/monitorings", async (ClaimsPrincipal principal, IAppDbContext db, CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            if (userId is null) return Results.Unauthorized();
            var list = await db.Monitorings.AsNoTracking()
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.UpdatedAt)
                .Select(m => new
                {
                    m.Id, m.Name, m.ProductUrl, m.ModelName, m.VoltageFilter,
                    m.TargetPrice, m.MaxPrice, m.ScheduleMinutes, m.SourceKind,
                    m.IsActive, m.DemoMode, m.LastCheckedAt, m.Cep
                })
                .ToListAsync(ct);
            return Results.Ok(list);
        });

        group.MapPost("/monitorings", async (
            [FromBody] CreateMonitoringRequest request,
            ClaimsPrincipal principal,
            IAppDbContext db,
            IUrlSafetyValidator urlValidator,
            CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            if (userId is null) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.ProductUrl))
                return Results.BadRequest(new { error = "Name and ProductUrl are required." });
            if (request.MaxPrice <= 0)
                return Results.BadRequest(new { error = "MaxPrice must be positive." });
            if (request.ScheduleMinutes is < 1 or > 10080)
                return Results.BadRequest(new { error = "ScheduleMinutes must be between 1 and 10080." });

            // Always validate URL shape for SSRF even in demo/mock.
            var safety = urlValidator.Validate(request.ProductUrl);
            if (!safety.IsValid)
                return Results.BadRequest(new { error = safety.Error });

            var entity = new Monitoring
            {
                UserId = userId.Value,
                Name = request.Name.Trim(),
                ProductUrl = request.ProductUrl.Trim(),
                ModelName = request.ModelName?.Trim(),
                VoltageFilter = request.VoltageFilter,
                TargetPrice = request.TargetPrice,
                MaxPrice = decimal.Round(request.MaxPrice, 2, MidpointRounding.AwayFromZero),
                Cep = request.Cep?.Trim(),
                ScheduleMinutes = request.ScheduleMinutes,
                SourceKind = request.SourceKind,
                DemoMode = request.DemoMode,
                IsActive = true
            };
            db.Monitorings.Add(entity);
            await db.SaveChangesAsync(ct);

            BackgroundJob.Enqueue<PriceCollectionJob>(j => j.CollectOneAsync(entity.Id));
            return Results.Created($"/api/monitorings/{entity.Id}", new { entity.Id });
        });

        group.MapGet("/monitorings/{id:guid}", async (
            Guid id,
            ClaimsPrincipal principal,
            IAppDbContext db,
            CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var m = await db.Monitorings.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
            if (m is null) return Results.NotFound();

            var observations = await db.Observations.AsNoTracking()
                .Include(o => o.Calculation)
                .Where(o => o.MonitoringId == id)
                .OrderByDescending(o => o.ObservedAt)
                .Take(100)
                .ToListAsync(ct);

            var alerts = await db.Deliveries.AsNoTracking()
                .Where(d => d.MonitoringId == id && d.UserId == userId)
                .OrderByDescending(d => d.CreatedAt)
                .Take(20)
                .ToListAsync(ct);

            var dto = new MonitoringDetailDto(
                m.Id, m.Name, m.ProductUrl, m.ModelName, m.VoltageFilter,
                m.TargetPrice, m.MaxPrice, m.Cep, m.ScheduleMinutes, m.SourceKind,
                m.IsActive, m.DemoMode, m.LastCheckedAt,
                observations.Select(o => new ObservationDto(
                    o.Id, o.ObservedAt, o.SourceName, o.Success, o.ProductTitle,
                    o.ListedPrice, o.VoltageDetected, o.VoltageMatches, o.ErrorMessage,
                    o.Calculation is null ? null : new PriceCalculationDto(
                        o.Calculation.ListedPrice, o.Calculation.Shipping, o.Calculation.CouponDiscount,
                        o.Calculation.PayAtCheckout, o.Calculation.CashbackEstimate,
                        o.Calculation.NetAfterCashback, o.Calculation.Notes))).ToList(),
                alerts.Select(a => new NotificationDeliveryDto(
                    a.Id, a.Channel, a.Status.ToString(), a.Subject, a.Body,
                    a.CreatedAt, a.DeliveredAt,
                    a.Status == DeliveryStatus.SkippedDuplicate)).ToList());

            return Results.Ok(dto);
        });

        group.MapPut("/monitorings/{id:guid}", async (
            Guid id,
            [FromBody] UpdateMonitoringRequest request,
            ClaimsPrincipal principal,
            IAppDbContext db,
            CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            if (userId is null) return Results.Unauthorized();
            var m = await db.Monitorings.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
            if (m is null) return Results.NotFound();

            if (request.MaxPrice <= 0)
                return Results.BadRequest(new { error = "MaxPrice must be positive." });

            m.Name = request.Name.Trim();
            m.ModelName = request.ModelName?.Trim();
            m.VoltageFilter = request.VoltageFilter;
            m.TargetPrice = request.TargetPrice;
            m.MaxPrice = decimal.Round(request.MaxPrice, 2, MidpointRounding.AwayFromZero);
            m.Cep = request.Cep?.Trim();
            m.ScheduleMinutes = request.ScheduleMinutes;
            m.IsActive = request.IsActive;
            m.DemoMode = request.DemoMode;
            m.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { m.Id });
        });

        group.MapDelete("/monitorings/{id:guid}", async (
            Guid id,
            ClaimsPrincipal principal,
            IAppDbContext db,
            CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            if (userId is null) return Results.Unauthorized();
            var m = await db.Monitorings.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
            if (m is null) return Results.NotFound();
            db.Monitorings.Remove(m);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        group.MapPost("/monitorings/{id:guid}/check-now", async (
            Guid id,
            ClaimsPrincipal principal,
            IAppDbContext db,
            CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            if (userId is null) return Results.Unauthorized();
            var exists = await db.Monitorings.AnyAsync(x => x.Id == id && x.UserId == userId, ct);
            if (!exists) return Results.NotFound();

            BackgroundJob.Enqueue<PriceCollectionJob>(j => j.CollectOneAsync(id));
            return Results.Accepted($"/api/monitorings/{id}", new { queued = true });
        });

        group.MapGet("/monitorings/{id:guid}/history", async (
            Guid id,
            ClaimsPrincipal principal,
            IAppDbContext db,
            [FromQuery] int days,
            CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            if (userId is null) return Results.Unauthorized();
            var owns = await db.Monitorings.AnyAsync(x => x.Id == id && x.UserId == userId, ct);
            if (!owns) return Results.NotFound();

            var window = days <= 0 ? 30 : Math.Min(days, 365);
            var since = DateTimeOffset.UtcNow.AddDays(-window);
            var rows = await db.Observations.AsNoTracking()
                .Include(o => o.Calculation)
                .Where(o => o.MonitoringId == id && o.ObservedAt >= since && o.Success)
                .OrderBy(o => o.ObservedAt)
                .Select(o => new
                {
                    o.ObservedAt,
                    o.ListedPrice,
                    PayAtCheckout = o.Calculation != null ? (decimal?)o.Calculation.PayAtCheckout : null,
                    CashbackEstimate = o.Calculation != null ? (decimal?)o.Calculation.CashbackEstimate : null,
                    NetAfterCashback = o.Calculation != null ? (decimal?)o.Calculation.NetAfterCashback : null
                })
                .ToListAsync(ct);
            return Results.Ok(rows);
        });

        return group;
    }
}
