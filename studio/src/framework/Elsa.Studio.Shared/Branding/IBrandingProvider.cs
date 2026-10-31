using Elsa.Studio.Branding.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Branding;

/// <summary>
/// Provides branding information for an application, including its name, logo, and reverse logo.
/// </summary>
public interface IBrandingProvider
{
    /// <summary>
    /// The name of the application.
    /// </summary>
    [Obsolete("Use Branding instead to provide a custom component, or configure the DefaultBrandingProvider with an AppName.")] string AppName { get; }

    /// <summary>
    /// The name of the application with the version.
    /// </summary>
    [Obsolete("Use Branding instead to provide a custom component, or configure the DefaultBrandingProvider with an AppNameWithVersion.")] string AppNameWithVersion { get; }

    /// <summary>
    /// The tagline of the application.
    /// </summary>
    string AppTagline { get; }

    /// <summary>
    /// Logo on a white background.
    /// </summary>
    [Obsolete("Use Branding instead to provide a custom component, or configure the DefaultBrandingProvider with a LogoUrl.")] string? LogoUrl { get; }

    /// <summary>
    /// Logo on a dark background
    /// </summary>
    [Obsolete("Use Branding instead to provide a custom component, or configure the DefaultBrandingProvider with a LogoReverseUrl.")] string? LogoReverseUrl { get; }

    /// <summary>
    /// The URL for the 16x16 favicon.
    /// </summary>
    string Favicon16Url { get; }

    /// <summary>
    /// The URL for the 32x32 favicon.
    /// </summary>
    string Favicon32Url { get; }

    /// <summary>
    /// The URL for the apple touch icon.
    /// </summary>
    string AppleTouchIconUrl { get; }

    /// <summary>
    /// The component to render. Use this to completely customize the branding component.
    /// </summary>
    RenderFragment Branding { get; }

    /// <summary>
    /// Represents branding configuration options for the login page.
    /// </summary>
    LoginBranding Login { get; }

    /// <summary>
    /// Represents the default app bar display icons options.
    /// </summary>
    DefaultAppBarIcons AppBarIcons { get; }
}