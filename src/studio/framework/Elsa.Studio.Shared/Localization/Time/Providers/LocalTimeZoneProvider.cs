namespace Elsa.Studio.Localization.Time.Providers;

/// <summary>
/// Provides the preferred time zone based on the server's local time zone.
/// </summary>
public class LocalTimeZoneProvider : ITimeZoneProvider
{
    /// <inheritdoc />
    public TimeZoneInfo GetTimeZone()
    {
        var timeZoneId = TimeZoneInfo.Local.Id;
        return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }
}