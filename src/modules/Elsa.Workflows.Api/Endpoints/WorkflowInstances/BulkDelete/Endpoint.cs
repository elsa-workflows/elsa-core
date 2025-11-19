using Elsa.Abstractions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.BulkDelete;

[PublicAPI]
internal class BulkDelete(IWorkflowRuntime workflowRuntime, IWorkflowInstanceStore workflowInstanceStore) : ElsaEndpoint<Request, Response>
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
        var filter = new WorkflowInstanceFilter
        {
            Ids = request.Ids,
            DefinitionId = request.WorkflowDefinitionId,
            DefinitionIds = request.WorkflowDefinitionIds,
        };
        
        // Get the list of instances to delete
        var instances = (await workflowInstanceStore.SummarizeManyAsync(filter, cancellationToken)).ToList();
        
        // Delete each instance using the runtime (to handle in-memory instances)
        foreach (var instance in instances)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            await workflowRuntime.DeleteWorkflowInstanceAsync(instance.Id, cancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        return new(instances.Count);   
    }
}