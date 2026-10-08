using System.Text.Json.Serialization;

namespace Elsa.Api.Client.Shared.Models;

/// <summary>
/// Represents a size.
/// </summary>
public class Size
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Size"/> class.
    /// </summary>
    [JsonConstructor]
    public Size()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Size"/> class.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public Size(double width, double height)
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