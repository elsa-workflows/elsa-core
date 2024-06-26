using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Graph.Segments;

internal class Request
{
    /// The workflow definition version ID.
    public string Id { get; set; } = default!;
    
    /// The ID of the leaf node. When set, its node and its ancestors will be returned.
    public string ChildNodeId { get; set; } = default!;
}

internal record Response(ActivityNode ChildNode, ActivityNode Container, ICollection<ActivityPathSegment> PathSegments);