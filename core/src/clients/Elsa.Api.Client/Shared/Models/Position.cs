using System.Text.Json.Serialization;

namespace Elsa.Api.Client.Shared.Models;

/// <summary>
/// Represents a position.
/// </summary>
public class Position
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Position"/> class.
    /// </summary>
    [JsonConstructor]
    public Position()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Position"/> class.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public Position(double x, double y)
    {
        X = x;
        Y = y;
    }
    
    /// <summary>
    /// Gets or sets the x coordinate.
    /// </summary>
    public double X { get; set; }
    
    /// <summary>
    /// Gets or sets the y coordinate.
    /// </summary>
    public double Y { get; set; }
}