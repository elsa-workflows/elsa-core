using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class WorkflowCancellationService(
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowInstanceStore workflowInstanceStore,
    IWorkflowCancellationDispatcher dispatcher)
    : IWorkflowCancellationService
{
    /// <inheritdoc />
    public Task<int> CancelWorkflowsAsync(IEnumerable<string> workflowInstanceIds, CancellationToken cancellationToken = default)
    {
        return CancelWorkflows(workflowInstanceIds, cancellationToken);
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
        var workflowDefinition = await workflowDefinitionService.FindAsync(definitionId, versionOptions, cancellationToken);
        if (workflowDefinition is null)
            return 0;

        return await CancelWorkflowByDefinitionVersionAsync(workflowDefinition.Id, cancellationToken);
    }

    private async Task<int> CancelWorkflows(IEnumerable<string> workflowInstanceIds, CancellationToken cancellationToken)
    {
        var ids = workflowInstanceIds.ToList();
        var tasks = new List<Task>();

        foreach (string workflowInstanceId in ids)
        {
            tasks.Add(dispatcher.DispatchAsync(new DispatchCancelWorkflowsRequest
            {
                WorkflowInstanceId = workflowInstanceId
            }, cancellationToken));
        }

        var childCount = await CancelChildWorkflowInstances(ids, cancellationToken);
        await Task.WhenAll(tasks);
        
        return tasks.Count + childCount;
    }

    private async Task<int> CancelChildWorkflowInstances(IEnumerable<string> workflowInstanceIds, CancellationToken cancellationToken)
    {
        var tasks = new List<Task<int>>();
        var workflowInstanceIdBatches = workflowInstanceIds.ToBatches(50);

        foreach (var workflowInstanceIdBatch in workflowInstanceIdBatches)
        {
            // Find child instances for the current workflow instance ID and cancel them.
            var filter = new WorkflowInstanceFilter
            {
                ParentWorkflowInstanceIds = workflowInstanceIdBatch.ToList(),
                WorkflowStatus = WorkflowStatus.Running
            };
            var childInstances = await workflowInstanceStore.FindManyAsync(filter, cancellationToken);

            tasks.AddRange(CancelWorkflows(childInstances.Select(c => c.Id), cancellationToken));
        }
        await Task.WhenAll(tasks);

        return tasks.Sum(t => t.Result);
    }
}