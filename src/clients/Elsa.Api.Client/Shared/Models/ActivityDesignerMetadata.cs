using System.Text.Json.Serialization;

namespace Elsa.Api.Client.Shared.Models;

/// <summary>
/// Represents designer metadata for an activity.
/// </summary>
public class ActivityDesignerMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityDesignerMetadata"/> class.
    /// </summary>
    [JsonConstructor]
    public ActivityDesignerMetadata()
    {
        
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityDesignerMetadata"/> class.
    /// </summary>
    /// <param name="x">The x coordinate of the activity.</param>
    /// <param name="y">The y coordinate of the activity.</param>
    /// <param name="width">The width of the activity.</param>
    /// <param name="height">The height of the activity.</param>
    public ActivityDesignerMetadata(double x, double y, double width, double height) : this(new Position(x, y), new Size(width, height))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityDesignerMetadata"/> class.
    /// </summary>
    /// <param name="position">The position of the activity.</param>
    /// <param name="size">The size of the activity.</param>
    public ActivityDesignerMetadata(Position position, Size size)
    {
        Position = position;
        Size = size;
    }

    /// <summary>
    /// Gets or sets the position of the activity.
    /// </summary>
    public Position Position { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the size of the activity.
    /// </summary>
    public Size Size { get; set; } = new();
}