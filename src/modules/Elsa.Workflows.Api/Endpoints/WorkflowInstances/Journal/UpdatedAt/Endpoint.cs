using Elsa.Abstractions;
using Elsa.Extensions;
using Elsa.Workflows.Api.Endpoints.WorkflowInstances.Journal.HasUpdates;
using Elsa.Workflows.Management.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Journal.UpdatedAt;

/// Endpoint that checks if there are updates for a workflow instance.
[PublicAPI]
internal class HasUpdates(IWorkflowInstanceStore store) : ElsaEndpoint<Request, Response>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/workflow-instances/{id}/updated-at");
        ConfigurePermissions("read:workflow-instances");
    }
    
    /// <inheritdoc />
    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var workflowInstance = await store.FindAsync(request.WorkflowInstanceId, cancellationToken);
        return new Response(workflowInstance!.UpdatedAt);
    }
}