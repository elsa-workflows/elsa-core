using Elsa.Abstractions;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Cancel;

[PublicAPI]
internal class Cancel(IWorkflowCancellationDispatcher workflowCancellationDispatcher)
    : ElsaEndpoint<Request>
{
    public override void Configure()
    {
        Post("/cancel/workflow-instances/{id}");
        ConfigurePermissions("cancel:workflow-instances");
        //Allows for post with empty body
        Description(x => x.Accepts<Request>("*/*"), clearDefaults: true);
    }
    
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        await workflowCancellationDispatcher.DispatchAsync(new DispatchCancelWorkflowsRequest
        {
            WorkflowInstanceIds = [request.Id]
        }, cancellationToken);
        
        await SendOkAsync(cancellationToken);
    }
}