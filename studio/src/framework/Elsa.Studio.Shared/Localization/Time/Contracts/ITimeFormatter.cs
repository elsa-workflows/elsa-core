namespace Elsa.Studio.Localization.Time;

/// <summary>
/// Formats a date and time value based on the preferred time zone provided by <see cref="ITimeZoneProvider"/>.
/// </summary>
public interface ITimeFormatter
{
    /// <summary>
    /// Formats the specified date and time value.
    /// </summary>
    /// <param name="value">The date and time value to format.</param>
    /// <param name="format">The format to use. Defaults to "G".</param>
    /// <param name="emptyString">The string to return when <paramref name="value"/> is <c>null</c>.</param>
    string Format(DateTimeOffset? value, string format = "G", string emptyString = "");
}