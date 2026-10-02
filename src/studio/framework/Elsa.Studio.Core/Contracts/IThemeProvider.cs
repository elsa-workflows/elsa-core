using MudBlazor;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides a theme to the dashboard.
/// </summary>
public interface IThemeProvider
{
    /// <summary>
    /// Gets the theme to use in the dashboard.
    /// </summary>
    MudTheme GetTheme();
}
