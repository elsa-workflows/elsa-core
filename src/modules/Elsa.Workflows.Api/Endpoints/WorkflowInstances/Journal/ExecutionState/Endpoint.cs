using Elsa.Abstractions;
using Elsa.Extensions;
using Elsa.Workflows.Management.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Journal.ExecutionState;

/// Endpoint that checks if there are updates for a workflow instance.
[PublicAPI]
internal class ExecutionState(IWorkflowInstanceStore store) : ElsaEndpoint<Request, Response>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/workflow-instances/{id}/execution-state");
        ConfigurePermissions("read:workflow-instances");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var workflowInstance = await store.FindAsync(request.WorkflowInstanceId, cancellationToken);

        if (workflowInstance == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var response = new Response(
            workflowInstance.Status,
            workflowInstance.SubStatus,
            workflowInstance.UpdatedAt);

        await SendOkAsync(response, cancellationToken);
    }
}