using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.UI.Contracts;
using JetBrains.Annotations;

namespace Elsa.Studio.Workflows.DiagramDesigners.Fallback;

/// <summary>
/// Provides fallback designer services.
/// </summary>
[UsedImplicitly]
public class FallbackDesignerProvider : IDiagramDesignerProvider
{
    /// <summary>
    /// Provides the priority.
    /// </summary>
    public double Priority => -1000;

    /// <inheritdoc />
    public bool IsFallback => true;

    /// <summary>
    /// Provides the get supports activity.
    /// </summary>
    public bool GetSupportsActivity(JsonObject activity) => true;

    /// <summary>
    /// Provides the get editor.
    /// </summary>
    public IDiagramDesigner GetEditor() => new FallbackDiagramDesigner();
}
