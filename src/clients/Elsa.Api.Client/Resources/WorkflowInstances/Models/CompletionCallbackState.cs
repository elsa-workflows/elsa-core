namespace Elsa.Api.Client.Resources.WorkflowInstances.Models;

public class CompletionCallbackState
{
    public CompletionCallbackState()
    {
    }

    public string OwnerInstanceId { get; init; } = default!;
    public string ChildNodeId { get; init; } = default!;
    public string? MethodName { get; init; } = default!;
}