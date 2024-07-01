using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Graph.Segments;

internal class Request
{
    /// The workflow definition version ID.
    public string Id { get; set; } = default!;
    
    /// The ID of the child node to get segments for.
    public string ChildNodeId { get; set; } = default!;
}

internal record Response(ActivityNode ChildNode, ActivityNode Container, ICollection<ActivityPathSegment> PathSegments);