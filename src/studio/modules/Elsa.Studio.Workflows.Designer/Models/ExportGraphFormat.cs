namespace Elsa.Studio.Workflows.Designer.Models;

/// <summary>
/// The image formats the designer can export its graph to.
/// </summary>
public enum ExportGraphFormat
{
    /// <summary>
    /// A raster PNG image.
    /// </summary>
    Png,

    /// <summary>
    /// A raster JPEG image.
    /// </summary>
    Jpeg,

    /// <summary>
    /// A vector SVG image.
    /// </summary>
    Svg
}
