using MudBlazor;
using MudBlazor.Utilities;

namespace Elsa.Studio.Workflows.Designer.Models;

/// <summary>
/// Theme colors used by X6-rendered workflow designer chrome.
/// </summary>
public sealed record X6DesignerTheme(
    string Grid,
    string Edge,
    string PortSurface,
    string PortStroke,
    string PortText,
    string Selection,
    string ConnectionHighlight,
    string EmbeddingHighlight)
{
    private const double GridOpacity = 0.42;

    /// <summary>
    /// Creates an X6 theme from the active MudBlazor palette.
    /// </summary>
    public static X6DesignerTheme FromPalette(Palette palette) =>
        new(
            ToHex(palette.TextSecondary.SetAlpha(GridOpacity)),
            ToHex(palette.LinesDefault),
            ToHex(palette.Surface),
            ToHex(palette.Primary),
            ToHex(palette.TextSecondary),
            ToHex(palette.Primary),
            ToHex(palette.Secondary),
            ToHex(palette.Success));

    private static string ToHex(MudColor color) => color.ToString(MudColorOutputFormats.HexA);
}
