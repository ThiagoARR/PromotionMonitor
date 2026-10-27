using System.Net;
using System.Net.Sockets;
using PromotionMonitor.Application.Abstractions;

namespace PromotionMonitor.Application.Services;

/// <summary>
/// Blocks private/link-local/loopback hosts and non-http(s) schemes (SSRF protection).
/// </summary>
public sealed class UrlSafetyValidator : IUrlSafetyValidator
{
    private static readonly HashSet<string> BlockedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost",
        "metadata.google.internal",
        "metadata.google",
        "169.254.169.254"
    };

    public UrlValidationResult Validate(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return new UrlValidationResult(false, "URL is required.");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return new UrlValidationResult(false, "URL must be absolute.");

        if (uri.Scheme is not ("http" or "https"))
            return new UrlValidationResult(false, "Only http and https URLs are allowed.");

        if (!string.IsNullOrEmpty(uri.UserInfo))
            return new UrlValidationResult(false, "URLs with userinfo are not allowed.");

        var host = uri.IdnHost;
        if (string.IsNullOrWhiteSpace(host))
            return new UrlValidationResult(false, "Host is required.");

        if (BlockedHosts.Contains(host))
            return new UrlValidationResult(false, "Host is blocked for SSRF protection.");

        if (IPAddress.TryParse(host, out var ip))
        {
            if (IsPrivateOrSpecial(ip))
                return new UrlValidationResult(false, "Private or special-use IP addresses are not allowed.");
        }
        else
        {
            // Reject obvious internal TLDs / suffixes without DNS resolution (avoid DNS rebinding in MVP).
            if (host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(".lan", StringComparison.OrdinalIgnoreCase))
            {
                return new UrlValidationResult(false, "Internal hostnames are not allowed.");
            }
        }

        return new UrlValidationResult(true, null);
    }

    private static bool IsPrivateOrSpecial(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
            return true;

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal)
                return true;
            var mapped = ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : null;
            if (mapped is not null)
                return IsPrivateOrSpecial(mapped);
            return false;
        }

        var bytes = ip.GetAddressBytes();
        return bytes[0] switch
        {
            0 => true,
            10 => true,
            127 => true,
            169 when bytes[1] == 254 => true,
            172 when bytes[1] >= 16 && bytes[1] <= 31 => true,
            192 when bytes[1] == 168 => true,
            100 when bytes[1] >= 64 && bytes[1] <= 127 => true, // CGNAT
            _ => false
        };
    }
}
