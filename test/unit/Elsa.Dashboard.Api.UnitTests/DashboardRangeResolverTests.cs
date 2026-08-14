using Elsa.Common;
using Elsa.Dashboard.Abstractions.Models;
using Elsa.Dashboard.Api.Services;

namespace Elsa.Dashboard.Api.UnitTests;

public class DashboardRangeResolverTests
{
    private readonly DateTimeOffset _now = new(2026, 06, 01, 12, 00, 00, TimeSpan.Zero);
    private readonly DashboardRangeResolver _resolver;

    public DashboardRangeResolverTests()
    {
        _resolver = new(new TestClock(_now));
    }

    [Theory]
    [InlineData("1h", DashboardRangeKeys.OneHour, 1)]
    [InlineData("24h", DashboardRangeKeys.TwentyFourHours, 24)]
    [InlineData("7d", DashboardRangeKeys.SevenDays, 168)]
    [InlineData(null, DashboardRangeKeys.TwentyFourHours, 24)]
    [InlineData("unknown", DashboardRangeKeys.TwentyFourHours, 24)]
    public void Resolve_ReturnsExpectedRange(string? input, string expectedKey, int expectedHours)
    {
        var range = _resolver.Resolve(input);

        Assert.Equal(expectedKey, range.Key);
        Assert.Equal(_now, range.To);
        Assert.Equal(_now.AddHours(-expectedHours), range.From);
    }

    [Theory]
    [InlineData(DashboardRangeKeys.OneHour, DashboardTrendGranularity.Minute)]
    [InlineData(DashboardRangeKeys.TwentyFourHours, DashboardTrendGranularity.Hour)]
    [InlineData(DashboardRangeKeys.SevenDays, DashboardTrendGranularity.Day)]
    public void ResolveGranularity_ChoosesDefaultForRange(string range, string expectedGranularity)
    {
        var granularity = _resolver.ResolveGranularity(null, range);

        Assert.Equal(expectedGranularity, granularity);
    }

    [Fact]
    public void ResolveGranularity_PreservesExplicitGranularity()
    {
        var granularity = _resolver.ResolveGranularity("custom", DashboardRangeKeys.OneHour);

        Assert.Equal("custom", granularity);
    }

    private class TestClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
