using Elsa.Common;
using Elsa.Dashboard.Api.Models;

namespace Elsa.Dashboard.Api.Services;

public class DashboardRangeResolver(ISystemClock clock)
{
    public DashboardRange Resolve(string? range)
    {
        var key = Normalize(range);
        var duration = key switch
        {
            DashboardRangeKeys.OneHour => TimeSpan.FromHours(1),
            DashboardRangeKeys.SevenDays => TimeSpan.FromDays(7),
            _ => TimeSpan.FromHours(24)
        };
        var to = clock.UtcNow;
        return new(key, to.Subtract(duration), to);
    }

    public string ResolveGranularity(string? granularity, string range) =>
        string.IsNullOrWhiteSpace(granularity)
            ? range switch
            {
                DashboardRangeKeys.OneHour => DashboardTrendGranularity.Minute,
                DashboardRangeKeys.SevenDays => DashboardTrendGranularity.Day,
                _ => DashboardTrendGranularity.Hour
            }
            : granularity;

    public TimeSpan GetBucketSize(string granularity) =>
        granularity.Equals(DashboardTrendGranularity.Minute, StringComparison.OrdinalIgnoreCase)
            ? TimeSpan.FromMinutes(5)
            : granularity.Equals(DashboardTrendGranularity.Day, StringComparison.OrdinalIgnoreCase)
                ? TimeSpan.FromDays(1)
                : TimeSpan.FromHours(1);

    private static string Normalize(string? range) =>
        range?.Trim().ToLowerInvariant() switch
        {
            DashboardRangeKeys.OneHour => DashboardRangeKeys.OneHour,
            DashboardRangeKeys.SevenDays => DashboardRangeKeys.SevenDays,
            _ => DashboardRangeKeys.TwentyFourHours
        };
}

public record DashboardRange(string Key, DateTimeOffset From, DateTimeOffset To);
