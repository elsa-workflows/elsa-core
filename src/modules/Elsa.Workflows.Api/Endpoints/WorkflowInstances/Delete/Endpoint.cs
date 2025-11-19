using Elsa.Abstractions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Delete;

[PublicAPI]
internal class Delete(IWorkflowRuntime workflowRuntime, IWorkflowInstanceManager workflowInstanceManager) : ElsaEndpoint<Request>
{
    public override void Configure()
    {
        Delete("/workflow-instances/{id}");
        ConfigurePermissions("delete:workflow-instances");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        // Check if the instance exists
        var exists = await workflowInstanceManager.ExistsAsync(request.Id, cancellationToken);
        
        if (!exists)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        // Use runtime deletion to handle in-memory instances and cleanup
#pragma warning disable CS0618 // Type or member is obsolete
        await workflowRuntime.DeleteWorkflowInstanceAsync(request.Id, cancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete
        
        await Send.NoContentAsync(cancellationToken);
    }
}