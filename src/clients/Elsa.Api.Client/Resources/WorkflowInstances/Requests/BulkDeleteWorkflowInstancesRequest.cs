namespace Elsa.Api.Client.Resources.WorkflowInstances.Requests;

public class BulkDeleteWorkflowInstancesRequest
{
    public ICollection<string> Ids { get; set; } = default!;
}