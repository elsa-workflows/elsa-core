using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Dispatch;

[PublicAPI]
internal class Endpoint : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowDispatcher _workflowDispatcher;
    private readonly IIdentityGenerator _identityGenerator;

    public Endpoint(IWorkflowDefinitionStore store, IWorkflowDispatcher workflowDispatcher, IIdentityGenerator identityGenerator)
    {
        _store = store;
        _workflowDispatcher = workflowDispatcher;
        _identityGenerator = identityGenerator;
    }

    public override void Configure()
    {
        Post("/workflow-definitions/{definitionId}/dispatch");
        ConfigurePermissions("exec:workflow-definitions");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var exists = await _store.AnyAsync(new WorkflowDefinitionFilter { DefinitionId = request.DefinitionId, VersionOptions = VersionOptions.Published }, cancellationToken);

        if (!exists)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var instanceId = request.InstanceId ?? _identityGenerator.GenerateId();
        var correlationId = request.CorrelationId;
        var triggerActivityId = request.TriggerActivityId;
        var input = (IDictionary<string, object>?)request.Input;
        var dispatchRequest = new DispatchWorkflowDefinitionRequest
        {
            DefinitionId = request.DefinitionId,
            VersionOptions = VersionOptions.Published,
            Input = input,
            InstanceId = instanceId,
            CorrelationId = correlationId,
            TriggerActivityId = triggerActivityId
        };

        await _workflowDispatcher.DispatchAsync(dispatchRequest, cancellationToken);

        await SendOkAsync(new Response(instanceId), cancellationToken);
    }
}