using Elsa.Studio.Branding.Models;
using Elsa.Studio.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Branding;

/// <summary>
/// Provides default branding information for the application, including the application name, logo URL, and reverse logo URL.
/// Implements the <see cref="Elsa.Studio.Branding.IBrandingProvider"/> interface.
/// </summary>
public class DefaultBrandingProvider : IBrandingProvider
{
    private LoginBranding LoginBranding { get; } = new()
    {
        BackgroundUrl = GetLoginBackgroundUrl(false),
        BackgroundReverseUrl = GetLoginBackgroundUrl(true)
    };

    private DefaultAppBarIcons AppBarIconsConfig { get; } = new()
    {
        ShowDocumentationLink = true,
        ShowGitHubLink = true
    };

    /// <inheritdoc />
    public virtual string AppName => "Elsa Studio";

    /// <inheritdoc />
    public virtual string AppNameWithVersion => $"{AppName} {ToolVersion.GetDisplayVersion()}";

    /// <inheritdoc />
    public virtual string AppTagline => "Build Workflows. Run Anything.";

    /// <inheritdoc />
    public virtual string LogoUrl => GetLogoUrl(false);

    /// <inheritdoc />
    public virtual string LogoReverseUrl => GetLogoUrl(true);

    /// <inheritdoc />
    public virtual string Favicon16Url => FindAppFileUrl("favicon-16x16.png", "_content/Elsa.Studio.Shell/");

    /// <inheritdoc />
    public virtual string Favicon32Url => FindAppFileUrl("favicon-32x32.png", "_content/Elsa.Studio.Shell/");

    /// <inheritdoc />
    public virtual string AppleTouchIconUrl => FindAppFileUrl("apple-touch-icon.png", "_content/Elsa.Studio.Shell/");

    /// <summary>
    /// Provides the render fragment.
    /// </summary>
    public virtual RenderFragment Branding =>
        builder =>
        {
            builder.OpenComponent(0, typeof(DefaultBranding));
            builder.AddAttribute(1, nameof(DefaultBranding.BrandingProvider), this);
            builder.CloseComponent();
        };

    /// <summary>
    /// Represents branding configuration options for the login page.
    /// </summary>
    public virtual LoginBranding Login => LoginBranding;

    /// <summary>
    /// Represents the default app bar display icons options.
    /// </summary>
    public virtual DefaultAppBarIcons AppBarIcons => AppBarIconsConfig;

    private string GetLogoUrl(bool reverse)
    {
        var fileName = reverse ? "icon_dark.png" : "icon.png";
        return FindAppFileUrl(fileName, "_content/Elsa.Studio.Shell/img/");
    }

    private static string GetLoginBackgroundUrl(bool reverse)
    {
        var fileName = reverse ? "login_background_dark.png" : "login_background_light.png";
        return FindAppFileUrl(fileName, "_content/Elsa.Studio.Shell/img/");
    }

    private static string FindAppFileUrl(string fileName, string fallbackPath)
    {
        var fallbackFile = $"{(fallbackPath ?? string.Empty).TrimEnd('/')}/{fileName}";
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", fileName),
            Path.Combine(AppContext.BaseDirectory ?? string.Empty, "wwwroot", "img", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "img", fileName)
        };

        var existing = candidates
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .FirstOrDefault(p => File.Exists(p));

        if (existing is not null) return "/img/" + fileName;

        return fallbackFile;
    }
}
