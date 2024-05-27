using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkDispatch;

[PublicAPI]
internal class Endpoint(IWorkflowDefinitionStore store, IWorkflowDispatcher workflowDispatcher, IIdentityGenerator identityGenerator)
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

        var exists = await store.AnyAsync(
            new WorkflowDefinitionFilter
            {
                DefinitionId = definitionId,
                VersionOptions = versionOptions
            },
            cancellationToken);

        if (!exists)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var instanceIds = new List<string>();

        for (var i = 0; i < request.Count; i++)
        {
            var instanceId = identityGenerator.GenerateId();
            var triggerActivityId = request.TriggerActivityId;
            var input = (IDictionary<string, object>?)request.Input;
            var dispatchRequest = new DispatchWorkflowDefinitionRequest
            {
                DefinitionId = definitionId,
                VersionOptions = versionOptions,
                Input = input,
                InstanceId = instanceId,
                TriggerActivityId = triggerActivityId
            };

            await workflowDispatcher.DispatchAsync(dispatchRequest, cancellationToken: cancellationToken);
            instanceIds.Add(instanceId);
        }

        await SendOkAsync(new Response(instanceIds), cancellationToken);
    }
}