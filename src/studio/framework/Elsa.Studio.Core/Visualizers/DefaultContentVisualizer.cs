using Elsa.Studio.Contracts;
using Elsa.Studio.Models;

namespace Elsa.Studio.Visualizers;

/// <summary>
/// Provides a default implementation of <see cref="IContentVisualizer"/>.
/// </summary>
public class DefaultContentVisualizer : IContentVisualizer
{
    /// <inheritdoc/>
    public string Name => "Default";

    /// <inheritdoc/>
    public string Syntax => "text";

    /// <inheritdoc/>
    public bool CanVisualize(object input) => true;

    /// <inheritdoc/>
    public string? ToPretty(object input)
    {
        return input?.ToString() ?? string.Empty;
    }

    /// <inheritdoc/>
    public TabulatedContentVisualizer? ToTable(object input) => null;
}