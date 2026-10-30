namespace Elsa.Expressions.Models;

/// <summary>
/// Represents a bracket pair.
/// </summary>
public class Brackets
{
    /// <summary>
    /// Gets the opening bracket.
    /// </summary>
    public char Open { get; }
    /// <summary>
    /// Gets the closing bracket.
    /// </summary>
    public char Close { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Brackets"/> class.
    /// </summary>
    /// <param name="open">The opening bracket.</param>
    /// <param name="close">The closing bracket.</param>
    public Brackets(char open, char close)
    {
        Open = open;
        Close = close;
    }

    /// <summary>
    /// An angle bracket pair.
    /// </summary>
    public static Brackets Angle => new('<', '>');
    
    /// <summary>
    /// A square bracket pair.
    /// </summary>
    public static Brackets Square => new('[', ']');
}