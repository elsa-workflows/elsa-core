using System.Globalization;

namespace Elsa.Studio.Localization.Options;

/// <summary>
/// Options for localization settings.
/// </summary>
public class LocalizationOptions
{
    /// <summary>
    /// Provides the localization section.
    /// </summary>
    public static string LocalizationSection = "Localization";

    /// <summary>
    /// Gets or sets the supported cultures.
    /// </summary>
    public string[] SupportedCultures { get; set; } = [];

    /// <summary>
    /// Gets or sets the default culture.
    /// </summary>
    public string DefaultCulture { get; set; } = "en-US";

    /// <summary>
    /// Gets or sets the culture picker formatter
    /// </summary>
    public Func<CultureInfo, string> CulturePickerFormatter { get; set; } = info => info.Name;
}