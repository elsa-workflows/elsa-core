using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Requests;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Dispatch;

[UsedImplicitly]
internal class Endpoint(IWorkflowDefinitionStore store, IWorkflowDispatcher workflowDispatcher, IIdentityGenerator identityGenerator) : ElsaEndpoint<Request, Response>
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

        var instanceId = request.InstanceId ?? identityGenerator.GenerateId();
        var dispatchRequest = new DispatchWorkflowDefinitionRequest
        {
            DefinitionId = definitionId,
            VersionOptions = versionOptions,
            Input = (IDictionary<string, object>?)request.Input,
            InstanceId = instanceId,
            CorrelationId = request.CorrelationId,
            TriggerActivityId = request.TriggerActivityId,
        };

        var options = new DispatchWorkflowOptions
        {
            Channel = request.Channel
        };

        await workflowDispatcher.DispatchAsync(dispatchRequest, options, cancellationToken);
        await SendOkAsync(new Response(instanceId), cancellationToken);
    }
}