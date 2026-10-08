namespace Elsa.Studio.Localization.Time.Providers;

/// <summary>
/// Provides the UTC time zone.
/// </summary>
public class UtcTimeZoneProvider : ITimeZoneProvider
{
    /// <inheritdoc />
    public TimeZoneInfo GetTimeZone()
    {
        return TimeZoneInfo.Utc;
    }
}