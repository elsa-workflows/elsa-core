using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Results;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class WorkflowInvoker(IWorkflowHostFactory workflowHostFactory) : IWorkflowInvoker
{
    /// <inheritdoc />
    public async Task<RunWorkflowResult> InvokeAsync(Workflow workflow, InvokeWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var instanceId = options?.WorkflowInstanceId;
        var workflowHost = await workflowHostFactory.CreateAsync(workflow, instanceId, cancellationToken);
        var runWorkflowParams = new RunWorkflowParams
        {
            Input = options?.Input,
            CorrelationId = options?.CorrelationId,
            Properties = options?.Properties,
            ParentWorkflowInstanceId = options?.ParentWorkflowInstanceId,
            TriggerActivityId = options?.TriggerActivityId
        };

        if (!await workflowHost.CanStartWorkflowAsync(runWorkflowParams, cancellationToken))
            throw new Exception("Workflow cannot be started.");

        return await workflowHost.RunWorkflowAsync(runWorkflowParams, cancellationToken);
    }
}