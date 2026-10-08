using Elsa.Studio.Authentication.UI.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Authentication.UI.Contracts;

/// <summary>
/// Creates the presentation for a login theme.
/// </summary>
public interface ILoginThemeProvider
{
    /// <summary>
    /// Renders the supplied shared login content within the theme's presentation.
    /// </summary>
    RenderFragment Render(LoginThemeContext context);
}
