using Elsa.Abstractions;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.BulkCancel;

/// <summary>
/// Represents an endpoint for bulk cancelling workflow instances.
/// </summary>
[PublicAPI]
public class BulkCancel(IWorkflowCancellationDispatcher workflowCancellationDispatcher) : ElsaEndpoint<Request, Response>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/bulk-actions/cancel/workflow-instances/");
        ConfigurePermissions("cancel:workflow-instances");
    }

    /// <inheritdoc />
    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var dispatchRequest = new DispatchCancelWorkflowsRequest
        {
            WorkflowInstanceIds = request.Ids,
            DefinitionVersionId = request.DefinitionVersionId,
            DefinitionId = request.DefinitionId,
            VersionOptions = request.VersionOptions
        };
        await workflowCancellationDispatcher.DispatchAsync(dispatchRequest, cancellationToken);
        
        //ToDo: how are we going to return feedback to the UI?
        return new(999);
    }
}