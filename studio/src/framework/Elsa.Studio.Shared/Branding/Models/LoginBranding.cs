namespace Elsa.Studio.Branding.Models;

/// <summary>
/// Represents branding configuration options for the login page.
/// </summary>
public class LoginBranding
{
    /// <summary>
    /// Login light background.
    /// </summary>
    public string BackgroundUrl { get; set; } = null!;

    /// <summary>
    /// Login dark background.
    /// </summary>
    public string BackgroundReverseUrl { get; set; } = null!;
}