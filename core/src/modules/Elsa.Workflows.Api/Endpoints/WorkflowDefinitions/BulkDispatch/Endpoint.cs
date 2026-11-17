using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Security;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkDispatch;

[PublicAPI]
internal class Endpoint(
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowDispatcher workflowDispatcher,
    IIdentityGenerator identityGenerator,
    WorkflowDefinitionScriptAuthorizationService scriptAuthorizationService)
    : ElsaEndpoint<Request, Response>
{
    private const int MaxInstancesPerRequest = 100;

    public override void Configure()
    {
        Post("/workflow-definitions/{definitionId}/bulk-dispatch");
        RequirePermission(Elsa.Workflows.Api.Permissions.WorkflowPermissions.Definitions, CoreVerbs.Execute);
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        if (request.Count is < 1 or > MaxInstancesPerRequest)
        {
            AddError(x => x.Count, $"Count must be between 1 and {MaxInstancesPerRequest}.");
            await Send.ErrorsAsync(cancellation: cancellationToken);
            return;
        }

        if (request.Input != null && request.Input is not IDictionary<string, object>)
        {
            AddError(x => x.Input, "Input must be a dictionary.");
            await Send.ErrorsAsync(cancellation: cancellationToken);
            return;
        }

        var definitionId = request.DefinitionId;
        var versionOptions = request.VersionOptions ?? VersionOptions.Published;
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(definitionId, versionOptions, cancellationToken);
        
        if (workflowGraph == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        var scriptAuthorizationResult = await scriptAuthorizationService.AuthorizeAsync(workflowGraph.Workflow, cancellationToken);
        if (!scriptAuthorizationResult.Succeeded)
        {
            await WorkflowDefinitionScriptAuthorizationFailure.SendAsync(scriptAuthorizationResult, message => AddError(message), Send.ErrorsAsync, cancellationToken);
            return;
        }

        var instanceIds = new List<string>(request.Count);
        var input = request.Input as IDictionary<string, object>;

        for (var i = 0; i < request.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var instanceId = identityGenerator.GenerateId();
            var dispatchRequest = new DispatchWorkflowDefinitionRequest(workflowGraph.Workflow.Identity.Id)
            {
                Input = input,
                InstanceId = instanceId,
                TriggerActivityId = request.TriggerActivityId
            };

            var result = await workflowDispatcher.DispatchAsync(dispatchRequest, cancellationToken: cancellationToken);
            if (!result.Succeeded)
            {
                AddError(result.Fault!.Message, result.Fault.Code);
                await Send.ErrorsAsync(cancellation: cancellationToken);
                return;
            }

            instanceIds.Add(instanceId);
        }

        await Send.OkAsync(new Response(instanceIds), cancellationToken);
    }

}
