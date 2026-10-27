namespace PromotionMonitor.Application.Abstractions;

public sealed record UrlValidationResult(bool IsValid, string? Error);

public interface IUrlSafetyValidator
{
    UrlValidationResult Validate(string url);
}
