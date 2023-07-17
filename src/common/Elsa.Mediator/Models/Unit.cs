namespace Elsa.Mediator.Models;

/// <summary>
/// Represents a void result.
/// </summary>
public record Unit
{
    /// <summary>
    /// Gets the singleton instance of <see cref="Unit"/>.
    /// </summary>
    public static readonly Unit Instance = new();
}