using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Designer.Models;

namespace Elsa.Studio.Workflows.Designer.Services;

internal sealed class X6DesignerThemeSubscription : IDisposable
{
    private readonly IThemeService _themeService;
    private readonly Func<X6DesignerTheme, Task> _applyTheme;

    public X6DesignerThemeSubscription(IThemeService themeService, Func<X6DesignerTheme, Task> applyTheme)
    {
        _themeService = themeService;
        _applyTheme = applyTheme;
        _themeService.CurrentThemeChanged += OnThemeChanged;
        _themeService.IsDarkModeChanged += OnThemeChanged;
    }

    public void Dispose()
    {
        _themeService.CurrentThemeChanged -= OnThemeChanged;
        _themeService.IsDarkModeChanged -= OnThemeChanged;
    }

    private async void OnThemeChanged() =>
        await _applyTheme(X6DesignerTheme.FromPalette(_themeService.CurrentPalette));
}
