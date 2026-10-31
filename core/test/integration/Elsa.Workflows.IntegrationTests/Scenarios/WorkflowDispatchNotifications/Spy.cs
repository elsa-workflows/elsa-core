using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowDispatchNotifications;

public class Spy
{
    private readonly TaskCompletionSource<bool> _workflowDefinitionDispatchingTcs = new();
    private readonly TaskCompletionSource<bool> _workflowDefinitionDispatchedTcs = new();
    private readonly TaskCompletionSource<bool> _workflowInstanceDispatchingTcs = new();
    private readonly TaskCompletionSource<bool> _workflowInstanceDispatchedTcs = new();

    public bool WorkflowDefinitionDispatchingWasCalled { get; set; }
    public bool WorkflowDefinitionDispatchedWasCalled { get; set; }
    public bool WorkflowInstanceDispatchingWasCalled { get; set; }
    public bool WorkflowInstanceDispatchedWasCalled { get; set; }
    
    public DispatchWorkflowDefinitionRequest? CapturedDefinitionRequest { get; set; }
    public DispatchWorkflowInstanceRequest? CapturedInstanceRequest { get; set; }
    public DispatchWorkflowResponse? CapturedResponse { get; set; }

    public Task WaitForWorkflowDefinitionDispatchingAsync() => _workflowDefinitionDispatchingTcs.Task;
    public Task WaitForWorkflowDefinitionDispatchedAsync() => _workflowDefinitionDispatchedTcs.Task;
    public Task WaitForWorkflowInstanceDispatchingAsync() => _workflowInstanceDispatchingTcs.Task;
    public Task WaitForWorkflowInstanceDispatchedAsync() => _workflowInstanceDispatchedTcs.Task;

    public void SignalWorkflowDefinitionDispatching() => _workflowDefinitionDispatchingTcs.TrySetResult(true);
    public void SignalWorkflowDefinitionDispatched() => _workflowDefinitionDispatchedTcs.TrySetResult(true);
    public void SignalWorkflowInstanceDispatching() => _workflowInstanceDispatchingTcs.TrySetResult(true);
    public void SignalWorkflowInstanceDispatched() => _workflowInstanceDispatchedTcs.TrySetResult(true);
}
