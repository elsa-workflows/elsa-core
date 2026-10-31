namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides content visualizers.
/// </summary>
public interface IContentVisualizerProvider
{
    /// <summary>
    /// Attempts to resolve the best matching visualizer for the input.
    /// </summary>
    /// <param name="content">The content to check visualizers against.</param>
    /// <returns></returns>
    IContentVisualizer MatchOrDefault(object content);

    /// <summary>
    /// Returns all content visualizers.
    /// </summary>
    /// <returns></returns>
    IEnumerable<IContentVisualizer> GetAll();
}