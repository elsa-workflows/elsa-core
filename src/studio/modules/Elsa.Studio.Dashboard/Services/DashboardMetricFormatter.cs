using System.Globalization;

namespace Elsa.Studio.Dashboard.Services;

public static class DashboardMetricFormatter
{
    public static string Count(long value) => value.ToString("N0", CultureInfo.CurrentCulture);

    public static string Count(int value) => Count((long)value);

    public static string Duration(TimeSpan? value)
    {
        if (value == null)
            return "N/A";

        var duration = value.Value;

        if (duration.TotalMilliseconds < 1000)
            return $"{duration.TotalMilliseconds:N0} ms";

        if (duration.TotalMinutes < 1)
            return $"{duration.TotalSeconds:N1} s";

        if (duration.TotalHours < 1)
            return $"{duration.TotalMinutes:N1} min";

        return $"{duration.TotalHours:N1} h";
    }

    public static string DateTime(DateTimeOffset? value)
    {
        return value == null ? "N/A" : value.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
    }

    public static string RelativeTimestamp(DateTimeOffset? value)
    {
        if (value == null)
            return "N/A";

        var elapsed = DateTimeOffset.UtcNow - value.Value.ToUniversalTime();

        if (elapsed.TotalSeconds < 60)
            return "Just now";

        if (elapsed.TotalMinutes < 60)
            return $"{elapsed.TotalMinutes:N0} min ago";

        if (elapsed.TotalHours < 24)
            return $"{elapsed.TotalHours:N0} h ago";

        return $"{elapsed.TotalDays:N0} d ago";
    }
}
