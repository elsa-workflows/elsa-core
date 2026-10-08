namespace Elsa.Studio.Authentication.UI.Models;

/// <summary>
/// Branding projected by the login host for use by presentation-only themes.
/// </summary>
public sealed record LoginThemeBranding(
    string ApplicationName,
    string? Tagline,
    string? LogoUrl,
    string? ReverseLogoUrl,
    string? ClassicBackgroundUrl,
    bool ShowDocumentationLink,
    bool ShowSourceLink);
