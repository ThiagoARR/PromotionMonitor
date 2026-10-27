using System.Text.Json;
using PromotionMonitor.Application.Abstractions;

namespace PromotionMonitor.Infrastructure.Shipping;

/// <summary>
/// Deterministic CEP-based shipping quotes for local/demo use when live credentials are absent.
/// Never invents a quote for invalid CEP — returns failure instead.
/// </summary>
public sealed class SimulatedShippingProvider : IShippingQuoteProvider
{
    public string Name => "SimulatedCep";

    public Task<ShippingQuoteResult> QuoteAsync(ShippingQuoteRequest request, CancellationToken cancellationToken = default)
    {
        var cep = NormalizeCep(request.Cep);
        if (cep is null)
        {
            return Task.FromResult(ShippingQuoteResult.Failed(
                Name, JsonSerializer.Serialize(new { error = "invalid_cep", cep = request.Cep }),
                "Invalid CEP; shipping not invented."));
        }

        // Deterministic: region digit + listed price bands.
        var region = cep[0] - '0';
        var baseRate = 12.90m + (region * 2.50m);
        var weightExtra = Math.Max(0m, request.WeightKg - 1m) * 4.50m;
        var valueExtra = request.ListedPrice >= 2000m ? 15.00m : request.ListedPrice >= 500m ? 5.00m : 0m;
        // Free shipping mock for high ticket in SP-ish CEPs (01xxx–05xxx) over R$350
        var amount = (region <= 5 && request.ListedPrice >= 350m)
            ? 0m
            : decimal.Round(baseRate + weightExtra + valueExtra, 2, MidpointRounding.AwayFromZero);

        var raw = JsonSerializer.Serialize(new
        {
            provider = Name,
            simulated = true,
            cep,
            listedPrice = request.ListedPrice,
            weightKg = request.WeightKg,
            amount
        });

        return Task.FromResult(new ShippingQuoteResult(true, amount, Name, Simulated: true, raw, null));
    }

    public static string? NormalizeCep(string? cep)
    {
        if (string.IsNullOrWhiteSpace(cep))
            return null;
        var digits = new string(cep.Where(char.IsDigit).ToArray());
        return digits.Length == 8 ? digits : null;
    }
}
