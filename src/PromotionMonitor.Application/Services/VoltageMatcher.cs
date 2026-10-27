using PromotionMonitor.Domain.Enums;

namespace PromotionMonitor.Application.Services;

public static class VoltageMatcher
{
    public static bool? Matches(Voltage filter, string? detected)
    {
        if (filter == Voltage.Any)
            return true;

        if (string.IsNullOrWhiteSpace(detected))
            return null;

        var raw = detected.Trim().ToUpperInvariant();
        var isDualHint = raw.Contains("BIVOLT", StringComparison.Ordinal) ||
                         raw.Contains("DUAL", StringComparison.Ordinal) ||
                         raw.Contains("110/220", StringComparison.Ordinal) ||
                         raw.Contains("127/220", StringComparison.Ordinal);

        var normalized = raw
            .Replace("BIVOLT", "DUAL", StringComparison.Ordinal)
            .Replace("VOLTS", string.Empty, StringComparison.Ordinal)
            .Replace("VOLT", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        var is110 = normalized.Contains("110", StringComparison.Ordinal) ||
                    normalized.Contains("127", StringComparison.Ordinal);
        var is220 = normalized.Contains("220", StringComparison.Ordinal) ||
                    normalized.Contains("240", StringComparison.Ordinal);
        var isDual = isDualHint ||
                     normalized.Contains("DUAL", StringComparison.Ordinal) ||
                     (is110 && is220);

        return filter switch
        {
            Voltage.Dual => isDual || (is110 && is220),
            Voltage.V110 => isDual || (is110 && !is220) || (is110 && is220),
            Voltage.V220 => isDual || (is220 && !is110) || (is110 && is220),
            _ => true
        };
    }
}
