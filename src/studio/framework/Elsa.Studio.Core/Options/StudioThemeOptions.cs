using Elsa.Studio.Models;

namespace Elsa.Studio.Options;

/// <summary>
/// Selects the application-wide Elsa Studio theme pack.
/// </summary>
public sealed class StudioThemeOptions
{
    /// <summary>
    /// The configuration section consumed by the theme-pack framework.
    /// </summary>
    public const string SectionName = "Presentation";

    /// <summary>
    /// Gets or sets the stable identifier of the selected theme pack.
    /// </summary>
    public string Theme { get; set; } = StudioThemeIds.HumanAutomation;
}
