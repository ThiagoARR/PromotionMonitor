namespace PromotionMonitor.Application.DTOs;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);
public sealed record LoginRequest(string Email, string Password);
public sealed record AuthResponse(Guid UserId, string Email, string DisplayName, string Token);
public sealed record NotificationPreferencesDto(bool PreferEmail, bool PreferConsole, string? NotificationEmail);
