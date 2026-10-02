namespace Elsa.Studio.Branding.Models;

/// <summary>
/// Gets the configuration for app bar icon visibility, controlling which default toolbar buttons are displayed.
/// </summary>
public class DefaultAppBarIcons
{
    /// <summary>
    /// Gets or sets a value indicating whether a link to documentation should be displayed in the user interface.
    /// </summary>
    public bool ShowDocumentationLink { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the GitHub link is displayed in the user interface.
    /// </summary>
    public bool ShowGitHubLink { get; set; }
}