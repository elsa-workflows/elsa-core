using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkDispatch;

[PublicAPI]
internal class Endpoint(IWorkflowDefinitionService workflowDefinitionService, IWorkflowDispatcher workflowDispatcher, IIdentityGenerator identityGenerator)
    : ElsaEndpoint<Request, Response>
{
    public override void Configure()
    {
        Post("/workflow-definitions/{definitionId}/bulk-dispatch");
        ConfigurePermissions("exec:workflow-definitions");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var definitionId = request.DefinitionId;
        var versionOptions = request.VersionOptions ?? VersionOptions.Published;
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(definitionId, versionOptions, cancellationToken);
        
        if (workflowGraph == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        var instanceIds = new List<string>();

        for (var i = 0; i < request.Count; i++)
        {
            var instanceId = identityGenerator.GenerateId();
            var triggerActivityId = request.TriggerActivityId;
            var input = (IDictionary<string, object>?)request.Input;
            var dispatchRequest = new DispatchWorkflowDefinitionRequest(workflowGraph.Workflow.Identity.Id)
            {
                Input = input,
                InstanceId = instanceId,
                TriggerActivityId = triggerActivityId
            };

            await workflowDispatcher.DispatchAsync(dispatchRequest, cancellationToken: cancellationToken);
            instanceIds.Add(instanceId);
        }

        await Send.OkAsync(new Response(instanceIds), cancellationToken);
    }
}