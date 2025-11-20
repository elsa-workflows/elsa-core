using Elsa.Abstractions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.BulkDelete;

[PublicAPI]
internal class BulkDelete(IWorkflowInstanceStore workflowInstanceStore, IWorkflowRuntime workflowRuntime) : ElsaEndpoint<Request, Response>
{
    public override void Configure()
    {
        Post(
            "/bulk-actions/delete/workflow-instances",
            "/bulk-actions/delete/workflow-instances/by-id" // Deprecated route.
        );
        ConfigurePermissions("delete:workflow-instances");
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var baseFilter = new WorkflowInstanceFilter
        {
            Ids = request.Ids,
            DefinitionId = request.WorkflowDefinitionId,
            DefinitionIds = request.WorkflowDefinitionIds,
        };

        // Step 1: Delete running instances individually (requires coordination by te workflow runtime).
        var runningFilter = new WorkflowInstanceFilter
        {
            Ids = baseFilter.Ids,
            DefinitionId = baseFilter.DefinitionId,
            DefinitionIds = baseFilter.DefinitionIds,
            WorkflowStatus = WorkflowStatus.Running
        };

        var runningInstanceIds = await workflowInstanceStore.FindManyIdsAsync(runningFilter, cancellationToken);
        var count = 0L;

        foreach (var instanceId in runningInstanceIds)
        {
            var client = await workflowRuntime.CreateClientAsync(instanceId, cancellationToken);
            var deleted = await client.DeleteAsync(cancellationToken);
            if (deleted)
                count++;
        }

        // Step 2: Bulk delete finished instances (no coordination needed).
        var finishedFilter = new WorkflowInstanceFilter
        {
            Ids = baseFilter.Ids,
            DefinitionId = baseFilter.DefinitionId,
            DefinitionIds = baseFilter.DefinitionIds,
            WorkflowStatus = WorkflowStatus.Finished
        };

        var finishedDeletedCount = await workflowInstanceStore.DeleteAsync(finishedFilter, cancellationToken);
        count += finishedDeletedCount;

        return new(count);
    }
}