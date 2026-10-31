using Elsa.Studio.Dashboard.Models;
using Elsa.Studio.Dashboard.Services;
using Xunit;

namespace Elsa.Studio.Dashboard.Tests;

public class DashboardRangeMapperTests
{
    [Theory]
    [InlineData("1h", DashboardRangeKeys.OneHour)]
    [InlineData("24h", DashboardRangeKeys.TwentyFourHours)]
    [InlineData("7d", DashboardRangeKeys.SevenDays)]
    [InlineData(null, DashboardRangeKeys.TwentyFourHours)]
    [InlineData("invalid", DashboardRangeKeys.TwentyFourHours)]
    public void Normalize_ReturnsSupportedRangeOrDefault(string? range, string expected)
    {
        var result = DashboardRangeMapper.Normalize(range);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1h", DashboardTrendGranularity.Minute)]
    [InlineData("24h", DashboardTrendGranularity.Hour)]
    [InlineData("7d", DashboardTrendGranularity.Day)]
    [InlineData("invalid", DashboardTrendGranularity.Hour)]
    public void GetDefaultGranularity_ReturnsRangeGranularity(string range, string expected)
    {
        var result = DashboardRangeMapper.GetDefaultGranularity(range);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1h", 1)]
    [InlineData("24h", 24)]
    [InlineData("invalid", 24)]
    public void GetUtcRange_ReturnsExpectedHourWindow(string range, int expectedHours)
    {
        var now = new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.FromHours(2));
        var result = DashboardRangeMapper.GetUtcRange(range, now);

        Assert.Equal(now.ToUniversalTime(), result.To);
        Assert.Equal(now.ToUniversalTime().AddHours(-expectedHours), result.From);
    }

    [Fact]
    public void GetUtcRange_ReturnsExpectedSevenDayWindow()
    {
        var now = new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.FromHours(2));
        var result = DashboardRangeMapper.GetUtcRange(DashboardRangeKeys.SevenDays, now);

        Assert.Equal(now.ToUniversalTime(), result.To);
        Assert.Equal(now.ToUniversalTime().AddDays(-7), result.From);
    }
}
