namespace Elsa.Workflows.Runtime;

public class TriggerInvoker(IWorkflowStarter workflowStarter) : ITriggerInvoker
{
    public async Task<StartWorkflowResponse> InvokeAsync(InvokeTriggerRequest request, CancellationToken cancellationToken = default)
    {
        var startRequest = new StartWorkflowRequest
        {
            Workflow = request.Workflow,
            CorrelationId = request.CorrelationId,
            Input = request.Input,
            Properties = request.Properties,
            ParentId = request.ParentWorkflowInstanceId,
            TriggerActivityId = request.ActivityId,
        };
                
        return await workflowStarter.StartWorkflowAsync(startRequest, cancellationToken);
    }
}