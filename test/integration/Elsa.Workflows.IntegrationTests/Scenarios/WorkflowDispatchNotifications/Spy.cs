using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowDispatchNotifications;

public class Spy
{
    public bool WorkflowDefinitionDispatchingWasCalled { get; set; }
    public bool WorkflowDefinitionDispatchedWasCalled { get; set; }
    public bool WorkflowInstanceDispatchingWasCalled { get; set; }
    public bool WorkflowInstanceDispatchedWasCalled { get; set; }
    
    public DispatchWorkflowDefinitionRequest? CapturedDefinitionRequest { get; set; }
    public DispatchWorkflowInstanceRequest? CapturedInstanceRequest { get; set; }
    public DispatchWorkflowResponse? CapturedResponse { get; set; }
}
