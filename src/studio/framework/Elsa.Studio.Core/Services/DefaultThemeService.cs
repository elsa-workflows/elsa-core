using Elsa.Studio.Contracts;
using MudBlazor;

namespace Elsa.Studio.Services;

/// <inheritdoc />
public class DefaultThemeService : IThemeService
{
    private readonly IThemeProvider _themeProvider;
    private MudTheme _currentTheme;
    private bool _isDarkMode;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultThemeService"/> class.
    /// </summary>
    public DefaultThemeService(IThemeProvider themeProvider)
    {
        _themeProvider = themeProvider;
        _currentTheme = themeProvider.GetTheme();
    }

    /// <inheritdoc />
    public event Action? CurrentThemeChanged;

    /// <inheritdoc />
    public event Action? IsDarkModeChanged;

    /// <inheritdoc />
    public MudTheme CurrentTheme
    {
        get => _currentTheme;
        set
        {
            _currentTheme = value;
            CurrentThemeChanged?.Invoke();
        }
    }

    /// <inheritdoc />
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            _isDarkMode = value;
            IsDarkModeChanged?.Invoke();
        }
    }
}