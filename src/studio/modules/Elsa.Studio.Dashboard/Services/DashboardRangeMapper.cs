using Elsa.Studio.Dashboard.Models;

namespace Elsa.Studio.Dashboard.Services;

public record DashboardRangeOption(string Value, string Label);
public record DashboardTimeRange(DateTimeOffset From, DateTimeOffset To);

public static class DashboardRangeMapper
{
    public static IReadOnlyList<DashboardRangeOption> Options { get; } =
    [
        new(DashboardRangeKeys.OneHour, "1h"),
        new(DashboardRangeKeys.TwentyFourHours, "24h"),
        new(DashboardRangeKeys.SevenDays, "7d")
    ];

    public static string Normalize(string? range)
    {
        return Options.Any(x => x.Value == range) ? range! : DashboardRangeKeys.TwentyFourHours;
    }

    public static string GetDefaultGranularity(string? range)
    {
        return Normalize(range) switch
        {
            DashboardRangeKeys.OneHour => DashboardTrendGranularity.Minute,
            DashboardRangeKeys.SevenDays => DashboardTrendGranularity.Day,
            _ => DashboardTrendGranularity.Hour
        };
    }

    public static DashboardTimeRange GetUtcRange(string? range, DateTimeOffset? now = null)
    {
        var to = (now ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var from = Normalize(range) switch
        {
            DashboardRangeKeys.OneHour => to.AddHours(-1),
            DashboardRangeKeys.SevenDays => to.AddDays(-7),
            _ => to.AddHours(-24)
        };

        return new(from, to);
    }
}
