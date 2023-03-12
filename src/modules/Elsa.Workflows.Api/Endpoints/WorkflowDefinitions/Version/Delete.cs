using Elsa.Workflows.Management.Contracts;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Version;

/// <summary>
/// Deletes a specific version of a workflow definition.
/// </summary>
[PublicAPI]
public class DeleteVersion : EndpointWithoutRequest
{
    private readonly IWorkflowDefinitionManager _workflowDefinitionManager;

    /// <inheritdoc />
    public DeleteVersion(IWorkflowDefinitionManager workflowDefinitionManager)
    {
        _workflowDefinitionManager = workflowDefinitionManager;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("workflow-definitions/{definitionId}/version/{version}");
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        var definitionId = Route<string>("definitionId")!;
        var version = Route<int>("version");
        
        var result = await _workflowDefinitionManager.DeleteVersionAsync(definitionId, version, ct);
        
        if (!result)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendOkAsync(ct);
    }
}