using Elsa.Studio.Contracts;
using Elsa.Studio.Visualizers;

namespace Elsa.Studio.Services;

/// <summary>
/// The default content visualizer provider.
/// </summary>
public class DefaultContentVisualizerProvider : IContentVisualizerProvider
{
    private readonly List<IContentVisualizer> _registeredVisualizers;
    private readonly IContentVisualizer _defaultVisualizer = new DefaultContentVisualizer();

    /// <summary>
    /// Initializes a new default instance of the <see cref="DefaultContentVisualizerProvider"/>.
    /// </summary>
    public DefaultContentVisualizerProvider(IEnumerable<IContentVisualizer> formatters) => _registeredVisualizers = formatters.ToList();

    /// <summary>
    /// Attempts to resolve the best matching visualizer for the input.
    /// Defaults to <see cref="DefaultContentVisualizer"/> Visualizer if none match.
    /// </summary>
    public IContentVisualizer MatchOrDefault(object input) => _registeredVisualizers.FirstOrDefault(f => f.CanVisualize(input)) ?? _defaultVisualizer;

    /// <summary>
    /// Returns all registered Visualizers, including the default Visualizer.
    /// </summary>
    public IEnumerable<IContentVisualizer> GetAll() => _registeredVisualizers.Concat([_defaultVisualizer]);
}