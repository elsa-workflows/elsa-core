using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class WorkflowCancellationService(
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowInstanceStore workflowInstanceStore,
    IWorkflowCancellationDispatcher dispatcher)
    : IWorkflowCancellationService
{
    /// <inheritdoc />
    public async Task<bool> CancelWorkflowAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowInstanceFilter
        {
            Id = workflowInstanceId
        };
        var instance = await workflowInstanceStore.FindAsync(filter, cancellationToken);

        if(instance == null)
            return false;
            
        await CancelWorkflows([instance], cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<int> CancelWorkflowsAsync(IEnumerable<string> workflowInstanceIds, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowInstanceFilter
        {
            Ids = workflowInstanceIds.ToList()
        };
        var instances = await workflowInstanceStore.FindManyAsync(filter, cancellationToken);
        return await CancelWorkflows(instances.ToList(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CancelWorkflowByDefinitionVersionAsync(string definitionVersionId, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowInstanceFilter
        {
            DefinitionVersionId = definitionVersionId,
            WorkflowStatus = WorkflowStatus.Running
        };
        var instances = (await workflowInstanceStore.FindManyAsync(filter, cancellationToken)).ToList();
        var instanceIds = instances.Select(i => i.Id).ToList();

        return await CancelWorkflowsAsync(instanceIds, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CancelWorkflowByDefinitionAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        // Shouldn't we get possible multiple definitions here?
        var workflowDefinition = await workflowDefinitionService.FindWorkflowDefinitionAsync(definitionId, versionOptions, cancellationToken);
        if (workflowDefinition is null)
            return 0;

        return await CancelWorkflowByDefinitionVersionAsync(workflowDefinition.Id, cancellationToken);
    }

    private async Task<int> CancelWorkflows(IList<WorkflowInstance> workflowInstances, CancellationToken cancellationToken)
    {
        var tasks = workflowInstances.Where(i => i.Status != WorkflowStatus.Finished)
            .Select(i => dispatcher.DispatchAsync(new DispatchCancelWorkflowRequest
            {
                WorkflowInstanceId = i.Id
            }, cancellationToken)).ToList();

        var instanceIds = workflowInstances.Select(i => i.Id).ToList();
        await CancelChildWorkflowInstances(instanceIds, cancellationToken);
        await Task.WhenAll(tasks);

        return tasks.Count;
    }

    private async Task CancelChildWorkflowInstances(IEnumerable<string> workflowInstanceIds, CancellationToken cancellationToken)
    {
        var tasks = new List<Task<int>>();
        var workflowInstanceIdBatches = workflowInstanceIds.Chunk(50);

        foreach (var workflowInstanceIdBatch in workflowInstanceIdBatches)
        {
            // Find child instances for the current workflow instance ID and cancel them.
            // Do not check on status as their children might still be running and need to be canceled.
            WorkflowInstanceFilter filter = new()
            {
                ParentWorkflowInstanceIds = workflowInstanceIdBatch.ToList()
            };
            var childInstances = (await workflowInstanceStore.FindManyAsync(filter, cancellationToken)).ToList();

            if (childInstances.Any())
                tasks.AddRange(CancelWorkflows(childInstances, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }
}