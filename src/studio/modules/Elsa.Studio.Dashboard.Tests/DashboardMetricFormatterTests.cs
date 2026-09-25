using System.Globalization;
using Elsa.Studio.Dashboard.Services;
using Xunit;

namespace Elsa.Studio.Dashboard.Tests;

public class DashboardMetricFormatterTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public DashboardMetricFormatterTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1200, "1,200")]
    public void Count_FormatsWholeNumbers(long value, string expected)
    {
        var result = DashboardMetricFormatter.Count(value);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, "N/A")]
    [InlineData(500, "500 ms")]
    [InlineData(1500, "1.5 s")]
    [InlineData(180000, "3.0 min")]
    [InlineData(7200000, "2.0 h")]
    public void Duration_FormatsUsefulUnit(int? milliseconds, string expected)
    {
        var duration = milliseconds == null ? (TimeSpan?)null : TimeSpan.FromMilliseconds(milliseconds.Value);

        var result = DashboardMetricFormatter.Duration(duration);

        Assert.Equal(expected, result);
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
    }
}
