using Elsa.Studio.Models;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Resolves the startup-selected Elsa Studio theme pack.
/// </summary>
public interface IStudioThemeRegistry
{
    /// <summary>
    /// Gets the selected theme registration.
    /// </summary>
    StudioThemeRegistration Selected { get; }

    /// <summary>
    /// Resolves the selected MudBlazor theme provider.
    /// </summary>
    IThemeProvider ResolveSelectedProvider();
}
