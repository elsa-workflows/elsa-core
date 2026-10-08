using Elsa.Studio.Contracts;
using MudBlazor;

namespace Elsa.Studio.Services;

/// <summary>
/// Adapts the selected Elsa Studio theme pack to the existing theme-provider contract.
/// </summary>
public class DefaultThemeProvider(IStudioThemeRegistry themeRegistry) : IThemeProvider
{
    /// <inheritdoc />
    public MudTheme GetTheme() => themeRegistry.ResolveSelectedProvider().GetTheme();
}
