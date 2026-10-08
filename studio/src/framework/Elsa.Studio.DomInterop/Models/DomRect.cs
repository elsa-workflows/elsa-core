namespace Elsa.Studio.DomInterop.Models;

/// <summary>
/// Represents the bounding rectangle returned by the DOM <c>getBoundingClientRect</c> API.
/// </summary>
public struct DomRect
{
    /// <summary>
    /// Gets or sets the x-coordinate of the rectangle's origin relative to the viewport.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Gets or sets the y-coordinate of the rectangle's origin relative to the viewport.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Gets or sets the width of the rectangle.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the rectangle.
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// Gets or sets the distance from the top of the viewport to the rectangle's top edge.
    /// </summary>
    public double Top { get; set; }

    /// <summary>
    /// Gets or sets the distance from the left of the viewport to the rectangle's right edge.
    /// </summary>
    public double Right { get; set; }

    /// <summary>
    /// Gets or sets the distance from the top of the viewport to the rectangle's bottom edge.
    /// </summary>
    public double Bottom { get; set; }

    /// <summary>
    /// Gets or sets the distance from the left of the viewport to the rectangle's left edge.
    /// </summary>
    public double Left { get; set; }
}