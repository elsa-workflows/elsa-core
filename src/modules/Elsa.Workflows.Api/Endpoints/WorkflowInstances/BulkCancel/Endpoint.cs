using Elsa.Abstractions;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.BulkCancel;

public class BulkCancel : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowRuntime _workflowRuntime;

    public BulkCancel(IWorkflowRuntime workflowRuntime)
    {
        _workflowRuntime = workflowRuntime;
    }

    public override void Configure()
    {
        Post("/bulk-actions/cancel/workflow-instances/by-id");
        ConfigurePermissions("cancel:workflow-instances");
    }
    
    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var tasks = request.Ids.Select(id => _workflowRuntime.CancelWorkflowAsync(id, cancellationToken)).ToList();
        await Task.WhenAll(tasks);

        var count = tasks.Count(t => t.IsCompletedSuccessfully);

        return new(count);
    }
}