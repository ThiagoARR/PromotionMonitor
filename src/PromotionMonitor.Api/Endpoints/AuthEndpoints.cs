using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PromotionMonitor.Application.Abstractions;
using PromotionMonitor.Application.DTOs;
using PromotionMonitor.Domain.Entities;
using PromotionMonitor.Infrastructure.Auth;

namespace PromotionMonitor.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (
            [FromBody] RegisterRequest request,
            IAppDbContext db,
            IJwtTokenService jwt,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { error = "Email and password are required." });
            if (request.Password.Length < 8)
                return Results.BadRequest(new { error = "Password must be at least 8 characters." });

            var email = request.Email.Trim().ToLowerInvariant();
            if (await db.Users.AnyAsync(u => u.Email == email, ct))
                return Results.Conflict(new { error = "Email already registered." });

            var user = new UserAccount
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? email.Split('@')[0] : request.DisplayName.Trim(),
                NotificationEmail = email,
                PreferEmailNotifications = true,
                PreferConsoleNotifications = true
            };
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);

            var token = jwt.CreateToken(user.Id, user.Email, user.DisplayName);
            return Results.Ok(new AuthResponse(user.Id, user.Email, user.DisplayName, token));
        }).RequireRateLimiting("auth");

        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            IAppDbContext db,
            IJwtTokenService jwt,
            CancellationToken ct) =>
        {
            var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
            if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password ?? string.Empty, user.PasswordHash))
                return Results.Unauthorized();

            var token = jwt.CreateToken(user.Id, user.Email, user.DisplayName);
            return Results.Ok(new AuthResponse(user.Id, user.Email, user.DisplayName, token));
        }).RequireRateLimiting("auth");

        group.MapGet("/me", async (ClaimsPrincipal principal, IAppDbContext db, CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            if (userId is null) return Results.Unauthorized();
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null) return Results.Unauthorized();
            return Results.Ok(new
            {
                user.Id,
                user.Email,
                user.DisplayName,
                preferences = new NotificationPreferencesDto(
                    user.PreferEmailNotifications,
                    user.PreferConsoleNotifications,
                    user.NotificationEmail)
            });
        }).RequireAuthorization();

        group.MapPut("/preferences", async (
            [FromBody] NotificationPreferencesDto prefs,
            ClaimsPrincipal principal,
            IAppDbContext db,
            CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            if (userId is null) return Results.Unauthorized();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null) return Results.Unauthorized();

            user.PreferEmailNotifications = prefs.PreferEmail;
            user.PreferConsoleNotifications = prefs.PreferConsole;
            user.NotificationEmail = string.IsNullOrWhiteSpace(prefs.NotificationEmail)
                ? user.Email
                : prefs.NotificationEmail.Trim();
            await db.SaveChangesAsync(ct);
            return Results.Ok(prefs);
        }).RequireAuthorization();

        return group;
    }

    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? principal.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
