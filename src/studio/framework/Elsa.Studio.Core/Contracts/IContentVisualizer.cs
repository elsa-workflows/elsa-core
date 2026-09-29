using Elsa.Studio.Models;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Defines a contract for visualizing objects into Visualized representations.
/// </summary>
public interface IContentVisualizer
{
    /// <summary>
    /// Gets the name of the content visualizer.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Returns the syntax used by this visualizer.
    /// </summary>
    string Syntax { get; }

    /// <summary>
    /// Determines whether the content can be visualized with this visualizer.
    /// </summary>
    /// <param name="input">The object to evaluate for visualizing capability. Cannot be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the input can be visualized; otherwise, <see langword="false"/>.</returns>
    bool CanVisualize(object input);

    /// <summary>
    /// Returns a prettified text version of the input.
    /// </summary>
    string? ToPretty(object input);

    /// <summary>
    /// Returns tabular representation of the input.
    /// Returns <see langword="null"/> if not supported.
    /// </summary>
    TabulatedContentVisualizer? ToTable(object input);
}