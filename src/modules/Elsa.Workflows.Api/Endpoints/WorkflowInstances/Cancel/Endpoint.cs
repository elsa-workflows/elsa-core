using Elsa.Abstractions;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Cancel;

[PublicAPI]
internal class Cancel : ElsaEndpoint<Request>
{
    private readonly IWorkflowRuntime _workflowRuntime;

    public Cancel(IWorkflowRuntime workflowRuntime)
    {
        _workflowRuntime = workflowRuntime;
    }

    public override void Configure()
    {
        Post("/cancel/workflow-instances/{id}");
        ConfigurePermissions("cancel:workflow-instances");
        //Allows for post with empty body
        Description(x => x.Accepts<Request>("*/*"), clearDefaults: true);
    }
    
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var result = await _workflowRuntime.CancelWorkflowAsync(request.Id, cancellationToken);
        
        if (result.Result)
            await SendOkAsync(cancellationToken);
        else if (result.Reason == FailureReason.NotFound)
            await SendNotFoundAsync(cancellationToken);
        else
        {
            if (result.Reason == FailureReason.InvalidState)
                AddError("Instance is in finished state.");
            else
                AddError("Unable to access instance.");
            
            await SendErrorsAsync(StatusCodes.Status409Conflict, cancellationToken);
        }
    }
}