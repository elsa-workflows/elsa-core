namespace Elsa.Studio.Localization.Time;

/// <summary>
/// Provides the preferred time zone.
/// </summary>
public interface ITimeZoneProvider
{
    /// <summary>
    /// Gets the preferred time zone.
    /// </summary>
    TimeZoneInfo GetTimeZone();
}