using Elsa.Studio.Localization.Options;

namespace Elsa.Studio.Localization.Models;

/// <summary>
/// Provides configuration hooks for customizing localization behavior.
/// </summary>
public class LocalizationConfig
{
    /// <summary>
    /// Gets or sets a delegate that configures localization options.
    /// </summary>
    public Action<LocalizationOptions> ConfigureLocalizationOptions { get; set; } = _ => { };
}