using FluentAssertions;
using PromotionMonitor.Application.Services;
using PromotionMonitor.Domain.Enums;

namespace PromotionMonitor.Tests;

public class VoltageMatcherTests
{
    [Theory]
    [InlineData(Voltage.Any, null, true)]
    [InlineData(Voltage.Any, "220V", true)]
    [InlineData(Voltage.V110, "110V", true)]
    [InlineData(Voltage.V110, "220V", false)]
    [InlineData(Voltage.V220, "220V", true)]
    [InlineData(Voltage.V220, "110V", false)]
    [InlineData(Voltage.Dual, "Bivolt", true)]
    [InlineData(Voltage.Dual, "110V", false)]
    [InlineData(Voltage.V110, "Bivolt", true)]
    [InlineData(Voltage.V220, null, null)]
    public void Matches_expected(Voltage filter, string? detected, bool? expected)
    {
        VoltageMatcher.Matches(filter, detected).Should().Be(expected);
    }
}
