using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.UI.Contracts;

/// <summary>
/// A service for managing diagram editors.
/// </summary>
public interface IDiagramDesignerService
{
    /// <summary>
    /// Returns whether a dedicated diagram designer is registered for the specified activity.
    /// </summary>
    bool HasDiagramDesigner(JsonObject activity);

    /// <summary>
    /// Gets the diagram designer for the specified activity.
    /// </summary>
    IDiagramDesigner GetDiagramDesigner(JsonObject activity);
}
