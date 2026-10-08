using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Graph.Segments;

internal class Request
{
    /// <summary>
    /// The workflow definition version ID.
    /// </summary>
    public string Id { get; set; } = default!;
    
    /// <summary>
    /// The ID of the child node to get segments for.
    /// </summary>
    public string ChildNodeId { get; set; } = default!;
}

internal record Response(ActivityNode ChildNode, ActivityNode Container, ICollection<ActivityPathSegment> PathSegments);