namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Graph;

internal class Request
{
    /// The workflow definition version ID.
    public string Id { get; set; } = default!;

    /// The ID of the node to select the subgraph for.
    public string ParentNodeId { get; set; } = default!;
}