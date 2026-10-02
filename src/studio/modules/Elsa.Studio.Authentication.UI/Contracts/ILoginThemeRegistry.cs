using Elsa.Studio.Authentication.UI.Models;

namespace Elsa.Studio.Authentication.UI.Contracts;

/// <summary>
/// Resolves the startup-selected login theme for the current scope.
/// </summary>
public interface ILoginThemeRegistry
{
    /// <summary>
    /// Gets the selected registration using its canonical identifier.
    /// </summary>
    LoginThemeRegistration Selected { get; }

    /// <summary>
    /// Resolves the selected presentation provider from the current scope.
    /// </summary>
    ILoginThemeProvider ResolveSelectedProvider();
}
