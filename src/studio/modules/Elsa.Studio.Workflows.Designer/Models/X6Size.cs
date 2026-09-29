using System.Text.Json.Serialization;

namespace Elsa.Studio.Workflows.Designer.Models;

/// <summary>
/// Represents the x6size.
/// </summary>
public class X6Size
{
    [JsonConstructor]
    public X6Size()
    {
    }
    
    public X6Size(double width, double height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    public double Width { get; set; }
    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    public double Height { get; set; }
}