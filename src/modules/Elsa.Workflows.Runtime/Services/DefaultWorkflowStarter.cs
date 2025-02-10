using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Exceptions;
using Elsa.Workflows.Runtime.Messages;

namespace Elsa.Workflows.Runtime;

public class DefaultWorkflowStarter(IWorkflowDefinitionService workflowDefinitionService, IWorkflowActivationStrategyEvaluator workflowActivationStrategyEvaluator, IWorkflowRuntime workflowRuntime) : IWorkflowStarter
{
    public async Task<StartWorkflowResponse> StartWorkflowAsync(StartWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        var workflow = await GetWorkflowAsync(request, cancellationToken);

        var canStart = await workflowActivationStrategyEvaluator.CanStartWorkflowAsync(new()
        {
            Workflow = workflow,
            CorrelationId = request.CorrelationId
        });

        if (!canStart)
            return new()
            {
                CannotStart = true
            };

        var workflowClient = await workflowRuntime.CreateClientAsync(cancellationToken);
        var createWorkflowInstanceRequest = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId(workflow.Identity.Id),
            CorrelationId = request.CorrelationId,
            Input = request.Input,
            TriggerActivityId = request.TriggerActivityId,
            ActivityHandle = request.ActivityHandle,
            Properties = request.Properties,
            ParentId = request.ParentId
        };

        var runWorkflowResponse = await workflowClient.CreateAndRunInstanceAsync(createWorkflowInstanceRequest, cancellationToken);
        return new()
        {
            CannotStart = false,
            WorkflowInstanceId = runWorkflowResponse.WorkflowInstanceId,
            Status = runWorkflowResponse.Status,
            SubStatus = runWorkflowResponse.SubStatus,
            Bookmarks = runWorkflowResponse.Bookmarks,
            Incidents = runWorkflowResponse.Incidents
        };
    }

    private async Task<Workflow> GetWorkflowAsync(StartWorkflowRequest request, CancellationToken cancellationToken)
    {
        if (request.Workflow != null)
            return request.Workflow;

        if (request.WorkflowDefinitionHandle == null)
            throw new InvalidOperationException("A workflow definition handle must be provided.");

        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(request.WorkflowDefinitionHandle, cancellationToken);

        if (workflowGraph == null)
            throw new WorkflowGraphNotFoundException("Workflow definition not found.", request.WorkflowDefinitionHandle);

        return workflowGraph.Workflow;
    }
}