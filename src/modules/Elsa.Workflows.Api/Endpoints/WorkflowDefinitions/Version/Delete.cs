using Elsa.Abstractions;
using Elsa.Workflows.Management;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Version;

/// <summary>
/// Deletes a specific version of a workflow definition.
/// </summary>
[PublicAPI]
public class DeleteVersion(IWorkflowDefinitionManager workflowDefinitionManager) : ElsaEndpointWithoutRequest
{
    /// <inheritdoc />
    public override void Configure()
    {
        Delete("workflow-definitions/{definitionId}/version/{version}");
        ConfigurePermissions("delete:workflow-definitions");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        var definitionId = Route<string>("definitionId")!;
        var version = Route<int>("version");
        
        var result = await workflowDefinitionManager.DeleteVersionAsync(definitionId, version, ct);
        
        if (!result)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendOkAsync(ct);
    }
}