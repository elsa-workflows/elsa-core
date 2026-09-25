namespace Elsa.Studio.Workflows.Designer.Models;

/// <summary>
/// Describes how to export the designer's graph as an image.
/// </summary>
public class ExportGraphOptions
{
    /// <summary>
    /// Gets or sets the image format to export. Defaults to <see cref="ExportGraphFormat.Png"/>.
    /// </summary>
    public ExportGraphFormat Format { get; set; } = ExportGraphFormat.Png;

    /// <summary>
    /// Gets or sets the name of the downloaded file, without an extension. The extension matching
    /// <see cref="Format"/> is appended by the designer.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the padding in pixels to leave around the exported content. Ignored when exporting to
    /// <see cref="ExportGraphFormat.Svg"/>, which has no raster canvas to pad.
    /// </summary>
    public int Padding { get; set; } = 20;
}
