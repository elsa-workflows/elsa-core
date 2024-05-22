using Elsa.Abstractions;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Management.Contracts;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Version;

[PublicAPI]
internal class RevertVersion : ElsaEndpointWithoutRequest
{
    private readonly IWorkflowDefinitionManager _workflowDefinitionManager;

    public RevertVersion(IWorkflowDefinitionManager workflowDefinitionManager)
    {
        _workflowDefinitionManager = workflowDefinitionManager;
    }

    public override void Configure()
    {
        Post("workflow-definitions/{definitionId}/revert/{version}");
        ConfigurePermissions("publish:workflow-definitions");
        Policies(AuthorizationPolicies.NotReadOnlyPolicy);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var definitionId = Route<string>("definitionId")!;
        var version = Route<int>("version");

        await _workflowDefinitionManager.RevertVersionAsync(definitionId, version, ct);
        
        await SendOkAsync(ct);
    }
}