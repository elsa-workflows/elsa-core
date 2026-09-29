using System.Globalization;

namespace Elsa.Studio.Localization.Services;

/// <summary>
/// Service to change the culture of the application.
/// </summary>
public interface ICultureService
{
    /// <summary>
    /// Change the culture of the application.
    /// </summary>
    Task ChangeCultureAsync(CultureInfo culture);
}