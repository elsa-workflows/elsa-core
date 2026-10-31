using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Studio.Workflows.UI.Contracts;

/// <summary>
/// An optional capability an <see cref="IDiagramDesigner"/> may implement when its canvas can show the activities of the
/// scopes nested in the one it displays, as a BPMN designer shows a subprocess's body inside the process. The diagram
/// wrapper then reports the state of those activities too, and selects one without opening its scope.
/// </summary>
/// <remarks>
/// Kept as a separate interface, rather than a member on <see cref="IDiagramDesigner"/> itself, so that every other
/// diagram designer, which shows one scope at a time, is untouched.
/// </remarks>
public interface INestedScopesDiagramDesigner
{
    /// <summary>Whether the canvas shows, along with <paramref name="container"/>'s own activities, those of the scopes nested in it.</summary>
    /// <param name="container">The scope the designer displays.</param>
    /// <param name="workflowDefinition">The workflow definition the scope belongs to, if known.</param>
    bool ShowsNestedScopes(JsonObject container, WorkflowDefinition? workflowDefinition);
}
