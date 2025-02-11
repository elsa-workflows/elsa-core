using Elsa.Abstractions;
using Elsa.Extensions;
using Elsa.Workflows.Management;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.ExecutionState;

/// <summary>
/// Returns the execution state of the specified workflow instance.
/// </summary>
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