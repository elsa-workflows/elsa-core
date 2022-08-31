using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Management.Services;
using FastEndpoints;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Version;

public class DeleteVersion : EndpointWithoutRequest
{
    private readonly IWorkflowDefinitionManager _workflowDefinitionManager;
    
    public DeleteVersion(IWorkflowDefinitionManager workflowDefinitionManager)
    {
        _workflowDefinitionManager = workflowDefinitionManager;
    }

    public override void Configure()
    {
        Delete("workflow-definitions/{definitionId}/version/{version}");
        AllowAnonymous();
    }

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