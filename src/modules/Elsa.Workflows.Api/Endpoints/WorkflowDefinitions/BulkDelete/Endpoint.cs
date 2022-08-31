using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Management.Services;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkDelete;

public class BulkDelete : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowDefinitionManager _workflowDefinitionManager;

    public BulkDelete(IWorkflowDefinitionManager workflowDefinitionManager, SerializerOptionsProvider serializerOptionsProvider)
    {
        _workflowDefinitionManager = workflowDefinitionManager;
    }

    public override void Configure()
    {
        Post("/bulk-actions/delete/workflow-definitions/by-definition-id");
        ConfigurePermissions("delete:workflow-definitions");
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var count = await _workflowDefinitionManager.BulkDeleteByDefinitionIdsAsync(request!.DefinitionIds, cancellationToken);
        return new Response(count);
    }
}