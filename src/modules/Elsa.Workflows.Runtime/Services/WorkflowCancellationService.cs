using Elsa.Common.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class WorkflowCancellationService(
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowInstanceStore workflowInstanceStore,
    IWorkflowRuntime workflowRuntime)
    : IWorkflowCancellationService
{
    /// <inheritdoc />
    public async Task<int> CancelWorkflowsAsync(IEnumerable<string> workflowInstanceIds, CancellationToken cancellationToken = default)
    {
        var tasks = workflowInstanceIds.Select(id => workflowRuntime.CancelWorkflowAsync(id, cancellationToken)).ToList();
        await Task.WhenAll(tasks);
        return tasks.Count(t => t.Result.Result);
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
        await Task.WhenAll(instances.Select(instance => workflowRuntime.CancelWorkflowAsync(instance.Id, cancellationToken)));

        return instances.Count;
    }

    /// <inheritdoc />
    public async Task<int> CancelWorkflowByDefinitionAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        // Shouldn't we get possible multiple definitions here?
        var workflowDefinition = await workflowDefinitionService.FindAsync(definitionId, versionOptions, cancellationToken);
        if (workflowDefinition is null)
            return 0;

        var result = await CancelWorkflowByDefinitionVersionAsync(workflowDefinition.Id, cancellationToken);
        return result;
    }
}