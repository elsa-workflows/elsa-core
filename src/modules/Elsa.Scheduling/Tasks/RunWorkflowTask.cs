using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scheduling.Tasks;

/// <summary>
/// A task that runs a workflow.
/// </summary>
public class RunWorkflowTask : ITask
{
    private readonly ScheduleNewWorkflowInstanceRequest _request;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunWorkflowTask"/> class.
    /// </summary>
    public RunWorkflowTask(ScheduleNewWorkflowInstanceRequest request)
    {
        _request = request;
    }
    
    /// <inheritdoc />
    public async ValueTask ExecuteAsync(TaskExecutionContext context)
    {
        var workflowRuntime = context.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
        var workflowClient = await workflowRuntime.CreateClientAsync();
        var request = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = _request.WorkflowDefinitionHandle,
            TriggerActivityId = _request.TriggerActivityId,
            Input = _request.Input,
            Properties = _request.Properties,
            ParentId = _request.ParentId,
            CorrelationId = _request.CorrelationId
        };
        await workflowClient.CreateAndRunInstanceAsync(request);
    }
}