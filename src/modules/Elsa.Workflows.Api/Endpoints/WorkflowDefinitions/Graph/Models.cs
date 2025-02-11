namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Graph;

internal class Request
{
    /// <summary>
    /// The workflow definition version ID.
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>
    /// The ID of the node to select the subgraph for.
    /// </summary>
    public string ParentNodeId { get; set; } = default!;
}