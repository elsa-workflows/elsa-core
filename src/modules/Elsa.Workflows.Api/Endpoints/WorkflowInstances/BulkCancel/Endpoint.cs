using Elsa.Abstractions;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.BulkCancel;

/// <summary>
/// Represents an endpoint for bulk cancelling workflow instances.
/// </summary>
[PublicAPI]
public class BulkCancel(IWorkflowCancellationService workflowCancellationService) : ElsaEndpoint<Request, Response>
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
        var tasks = new List<Task<int>>();
        
        if (request.Ids is not null)
            tasks.Add(workflowCancellationService.CancelWorkflowsAsync(request.Ids!, cancellationToken));
        if (request.DefinitionVersionId is not null)
            tasks.Add(workflowCancellationService.CancelWorkflowByDefinitionVersionAsync(request.DefinitionVersionId!, cancellationToken));
        if (request.DefinitionId is not null)
            tasks.Add(workflowCancellationService.CancelWorkflowByDefinitionAsync(request.DefinitionId!, request.VersionOptions!.Value, cancellationToken));

        await Task.WhenAll(tasks);

        return new(tasks.Sum(t => t.Result));
    }
}