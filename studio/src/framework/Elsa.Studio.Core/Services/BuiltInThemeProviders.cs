using Elsa.Studio.Contracts;
using MudBlazor;

namespace Elsa.Studio.Services;

/// <summary>
/// The original Elsa Studio blue theme.
/// </summary>
public sealed class ClassicThemeProvider : IThemeProvider
{
    public MudTheme GetTheme() => StudioThemeFactory.Create(
        primary: "#0EA5E9",
        secondary: "#64748B",
        darkSecondary: "#AAB7CA",
        lightBackground: "#FFFFFF",
        lightSurface: "#F8FAFC",
        lightDrawer: "#F8FAFC",
        lightText: "#0F172A",
        lightSecondaryText: "#64748B",
        lightDivider: "#E2E8F0",
        darkBackground: "#0F172A",
        darkSurface: "#182234",
        darkDrawer: "#0F172A",
        darkText: "#F8FAFC",
        darkSecondaryText: "#AAB7CA",
        darkDivider: "#334155");
}

/// <summary>
/// A signal-rich blue theme inspired by connected workflow constellations.
/// </summary>
public sealed class WorkflowConstellationThemeProvider : IThemeProvider
{
    public MudTheme GetTheme() => StudioThemeFactory.Create(
        primary: "#315EBD",
        secondary: "#6E56CF",
        darkSecondary: "#A99CF5",
        lightBackground: "#F5F8FF",
        lightSurface: "#FFFFFF",
        lightDrawer: "#EFF4FF",
        lightText: "#17233B",
        lightSecondaryText: "#60708D",
        lightDivider: "#D7E0F2",
        darkBackground: "#07142E",
        darkSurface: "#0C1D3D",
        darkDrawer: "#091833",
        darkText: "#F5F8FF",
        darkSecondaryText: "#A9B9D6",
        darkDivider: "#29416C");
}

/// <summary>
/// A crisp, luminous theme for higher-energy workflow environments.
/// </summary>
public sealed class WorkflowAuroraThemeProvider : IThemeProvider
{
    public MudTheme GetTheme() => StudioThemeFactory.Create(
        primary: "#0868BD",
        secondary: "#087F8C",
        darkSecondary: "#6BC6CC",
        lightBackground: "#F4FAFD",
        lightSurface: "#FFFFFF",
        lightDrawer: "#ECF7FB",
        lightText: "#122430",
        lightSecondaryText: "#58717F",
        lightDivider: "#D1E4EA",
        darkBackground: "#071B26",
        darkSurface: "#0D2733",
        darkDrawer: "#09212C",
        darkText: "#F1FAFC",
        darkSecondaryText: "#A7C0C9",
        darkDivider: "#284653");
}

/// <summary>
/// A compact, evidence-oriented theme for execution-heavy environments.
/// </summary>
public sealed class ExecutionTimelineThemeProvider : IThemeProvider
{
    public MudTheme GetTheme() => StudioThemeFactory.Create(
        primary: "#3977B7",
        secondary: "#596B7D",
        darkSecondary: "#AEBAC6",
        lightBackground: "#F5F7F9",
        lightSurface: "#FFFFFF",
        lightDrawer: "#F0F3F6",
        lightText: "#202A35",
        lightSecondaryText: "#637181",
        lightDivider: "#D9E0E7",
        darkBackground: "#10161D",
        darkSurface: "#18212B",
        darkDrawer: "#141B23",
        darkText: "#F3F6F8",
        darkSecondaryText: "#AFBAC5",
        darkDivider: "#35414D");
}

/// <summary>
/// The warm-professional Elsa Studio theme optimized for human handoffs.
/// </summary>
public sealed class HumanAutomationThemeProvider : IThemeProvider
{
    public MudTheme GetTheme() => StudioThemeFactory.Create(
        primary: "#245EA8",
        secondary: "#A15C38",
        darkSecondary: "#D89A78",
        lightBackground: "#F7F4EF",
        lightSurface: "#FFFCF8",
        lightDrawer: "#F1ECE4",
        lightText: "#252B33",
        lightSecondaryText: "#5F6875",
        lightDivider: "#DDD5CA",
        darkBackground: "#111820",
        darkSurface: "#18212B",
        darkDrawer: "#141C25",
        darkText: "#F4F1EC",
        darkSecondaryText: "#AEB7C3",
        darkDivider: "#34404D");
}

internal static class StudioThemeFactory
{
    public static MudTheme Create(
        string primary,
        string secondary,
        string darkSecondary,
        string lightBackground,
        string lightSurface,
        string lightDrawer,
        string lightText,
        string lightSecondaryText,
        string lightDivider,
        string darkBackground,
        string darkSurface,
        string darkDrawer,
        string darkText,
        string darkSecondaryText,
        string darkDivider)
    {
        return new MudTheme
        {
            LayoutProperties =
            {
                DefaultBorderRadius = "6px"
            },
            PaletteLight =
            {
                Primary = primary,
                Secondary = secondary,
                Info = "#3977B7",
                Success = "#2E7D5B",
                Warning = "#B7791F",
                Error = "#C84646",
                Background = lightBackground,
                Surface = lightSurface,
                DrawerBackground = lightDrawer,
                DrawerText = lightText,
                AppbarBackground = lightSurface,
                AppbarText = lightText,
                TextPrimary = lightText,
                TextSecondary = lightSecondaryText,
                Divider = lightDivider,
                LinesDefault = lightDivider,
                LinesInputs = lightDivider
            },
            PaletteDark =
            {
                Primary = "#78AEEE",
                Secondary = darkSecondary,
                Info = "#75AADB",
                Success = "#62B88F",
                Warning = "#E0AB53",
                Error = "#E47777",
                Background = darkBackground,
                Surface = darkSurface,
                DrawerBackground = darkDrawer,
                DrawerText = darkText,
                AppbarBackground = darkSurface,
                AppbarText = darkText,
                TextPrimary = darkText,
                TextSecondary = darkSecondaryText,
                Divider = darkDivider,
                LinesDefault = darkDivider,
                LinesInputs = darkDivider
            }
        };
    }
}
