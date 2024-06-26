namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Graph;

internal class Request
{
    /// The workflow definition version ID.
    public string Id { get; set; } = default!;

    /// The ID of the parent node. When set, its node and its descendants will be returned.
    public string ParentNodeId { get; set; } = default!;
}