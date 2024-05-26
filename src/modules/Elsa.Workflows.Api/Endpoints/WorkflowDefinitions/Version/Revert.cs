using Elsa.Abstractions;
using Elsa.Workflows.Management;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Version;

[PublicAPI]
internal class RevertVersion(IWorkflowDefinitionManager workflowDefinitionManager) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Post("workflow-definitions/{definitionId}/revert/{version}");
        ConfigurePermissions("publish:workflow-definitions");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var definitionId = Route<string>("definitionId")!;
        var version = Route<int>("version");

        await workflowDefinitionManager.RevertVersionAsync(definitionId, version, ct);
        
        await SendOkAsync(ct);
    }
}