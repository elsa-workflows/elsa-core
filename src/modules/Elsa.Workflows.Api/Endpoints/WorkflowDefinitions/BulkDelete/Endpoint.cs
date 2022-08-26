using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Management.Services;
using FastEndpoints;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkDelete;

public class BulkDelete : Endpoint<Request, Response>
{
    private readonly IWorkflowDefinitionManager _workflowDefinitionManager;

    public BulkDelete(IWorkflowDefinitionManager workflowDefinitionManager, SerializerOptionsProvider serializerOptionsProvider)
    {
        _workflowDefinitionManager = workflowDefinitionManager;
    }

    public override void Configure()
    {
        Post("/bulk-actions/delete/workflow-definitions/by-definition-id");
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var count = await _workflowDefinitionManager.BulkDeleteByDefinitionIdsAsync(request!.DefinitionIds, cancellationToken);
        return new Response(count);
    }
}