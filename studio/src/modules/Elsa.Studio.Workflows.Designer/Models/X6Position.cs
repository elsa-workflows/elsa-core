using System.Text.Json.Serialization;

namespace Elsa.Studio.Workflows.Designer.Models;

/// <summary>
/// Represents the x6position.
/// </summary>
public class X6Position
{
    [JsonConstructor]
    public X6Position()
    {
    }

    public X6Position(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Gets or sets the x.
    /// </summary>
    public double X { get; set; }
    /// <summary>
    /// Gets or sets the y.
    /// </summary>
    public double Y { get; set; }
}