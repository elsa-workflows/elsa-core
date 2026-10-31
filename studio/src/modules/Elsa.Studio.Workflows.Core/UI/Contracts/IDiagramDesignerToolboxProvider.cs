using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.UI.Contracts;

/// <summary>
/// Implement this interface to provide toolbox items for the diagram editor.
/// </summary>
public interface IDiagramDesignerToolboxProvider : IDiagramDesigner
{
    IEnumerable<RenderFragment> GetToolboxItems(bool isReadOnly);
}