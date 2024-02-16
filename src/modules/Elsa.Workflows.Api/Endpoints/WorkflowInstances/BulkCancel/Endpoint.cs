using Elsa.Abstractions;
using Elsa.Workflows.Runtime.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.BulkCancel;

[PublicAPI]
public class BulkCancel : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowRuntime _workflowRuntime;

    public BulkCancel(IWorkflowRuntime workflowRuntime)
    {
        _workflowRuntime = workflowRuntime;
    }

    public override void Configure()
    {
        Post("/bulk-actions/cancel/workflow-instances/");
        ConfigurePermissions("cancel:workflow-instances");
    }
    
    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var tasks = new List<Task<int>>();
        
        if (request.Ids is not null)
            tasks.Add(_workflowRuntime.CancelWorkflowAsync(request.Ids!, cancellationToken));
        if (request.DefinitionVersionId is not null)
            tasks.Add(_workflowRuntime.CancelWorkflowByDefinitionVersionAsync(request.DefinitionVersionId!, cancellationToken));
        if (request.DefinitionId is not null)
            tasks.Add(_workflowRuntime.CancelWorkflowByDefinitionAsync(request.DefinitionId!, request.VersionOptions!.Value, cancellationToken));

        await Task.WhenAll(tasks);

        return new(tasks.Sum(t => t.Result));
    }
}