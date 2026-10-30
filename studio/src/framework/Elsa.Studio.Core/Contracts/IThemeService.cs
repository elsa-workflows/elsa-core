using MudBlazor;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides theme information to the dashboard.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Raised when the current theme changes.
    /// </summary>
    event Action CurrentThemeChanged;
    
    /// <summary>
    /// Raised when the dark mode changes.
    /// </summary>
    event Action IsDarkModeChanged;

    /// <summary>
    /// The current theme.
    /// </summary>
    MudTheme CurrentTheme { get; set; }
    
    /// <summary>
    /// Returns the current palette, depending on whether the dashboard is in dark mode.
    /// </summary>
#pragma warning disable CS0618
    Palette CurrentPalette => IsDarkMode ? CurrentTheme.PaletteDark : CurrentTheme.PaletteLight;
#pragma warning restore CS0618

    /// <summary>
    /// Whether the dashboard is in dark mode.
    /// </summary>
    public bool IsDarkMode { get; set; }
}