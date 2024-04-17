using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Dispatch;

[UsedImplicitly]
internal class Endpoint(IWorkflowDefinitionService workflowDefinitionService, IWorkflowDispatcher workflowDispatcher, IIdentityGenerator identityGenerator) : ElsaEndpoint<Request, Response>
{
    public override void Configure()
    {
        Post("/workflow-definitions/{definitionId}/dispatch");
        ConfigurePermissions("exec:workflow-definitions");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var definitionId = request.DefinitionId;
        var versionOptions = request.VersionOptions ?? VersionOptions.Published;
        var workflow = await workflowDefinitionService.FindWorkflowAsync(definitionId, versionOptions, cancellationToken);

        if(workflow == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }
        
        var input = request.Input;
        
        if(input != null && input is not IDictionary<string, object>)
        {
            AddError("Input must be a dictionary.");
            await SendErrorsAsync(cancellation: cancellationToken);
            return;
        }

        var instanceId = request.InstanceId ?? identityGenerator.GenerateId();
        var dispatchRequest = new DispatchWorkflowDefinitionRequest(workflow.Identity.Id)
        {
            Input = input as IDictionary<string, object>,
            InstanceId = instanceId,
            CorrelationId = request.CorrelationId,
            TriggerActivityId = request.TriggerActivityId,
        };

        var options = new DispatchWorkflowOptions
        {
            Channel = request.Channel
        };

        var result = await workflowDispatcher.DispatchAsync(dispatchRequest, options, cancellationToken);
        
        if(!result.Succeeded)
        {
            AddError(result.ErrorMessage!);
            await SendErrorsAsync(cancellation: cancellationToken);
            return;
        }
        
        await SendOkAsync(new Response(instanceId), cancellationToken);
    }
}