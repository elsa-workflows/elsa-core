using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Authentication.UI.Models;

/// <summary>
/// Shared, presentation-only content supplied to a login theme.
/// </summary>
public sealed record LoginThemeContext(
    LoginThemeBranding Branding,
    RenderFragment LoginPanel,
    RenderFragment UtilityLinks,
    string Version);
