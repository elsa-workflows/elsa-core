using Elsa.Studio.Authentication.UI.Models;
using Elsa.Studio.Branding;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Authentication.UI.Services;

/// <summary>
/// Projects application services into the presentation-only theme context.
/// </summary>
public sealed class LoginThemeContextFactory
{
    private readonly IClientInformationProvider _clientInformationProvider;

#pragma warning disable CS0618
    public LoginThemeContextFactory(
        IBrandingProvider brandingProvider,
        IClientInformationProvider clientInformationProvider)
    {
        _clientInformationProvider = clientInformationProvider;
        Branding = new(
            brandingProvider.AppName,
            brandingProvider.AppTagline,
            brandingProvider.LogoUrl,
            brandingProvider.LogoReverseUrl,
            brandingProvider.Login.BackgroundUrl,
            brandingProvider.AppBarIcons.ShowDocumentationLink,
            brandingProvider.AppBarIcons.ShowGitHubLink);
    }
#pragma warning restore CS0618

    /// <summary>
    /// Gets the projected host branding.
    /// </summary>
    public LoginThemeBranding Branding { get; }

    /// <summary>
    /// Gets the host application name used by the shared login panel.
    /// </summary>
    public string ApplicationName => Branding.ApplicationName;

    /// <summary>
    /// Creates a complete context with the installed Studio version.
    /// </summary>
    public async ValueTask<LoginThemeContext> CreateAsync(
        RenderFragment loginPanel,
        RenderFragment utilityLinks,
        CancellationToken cancellationToken = default)
    {
        var client = await _clientInformationProvider.GetInfoAsync(cancellationToken);
        return new(Branding, loginPanel, utilityLinks, client.PackageVersion);
    }
}
