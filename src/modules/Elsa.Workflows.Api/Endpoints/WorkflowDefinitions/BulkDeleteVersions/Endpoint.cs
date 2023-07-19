using Elsa.Abstractions;
using Elsa.Workflows.Management.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkDeleteVersions;

[PublicAPI]
internal class BulkDeleteVersions : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowDefinitionManager _workflowDefinitionManager;

    public BulkDeleteVersions(IWorkflowDefinitionManager workflowDefinitionManager)
    {
        _workflowDefinitionManager = workflowDefinitionManager;
    }

    public override void Configure()
    {
        Post("/bulk-actions/delete/workflow-definitions/by-id");
        ConfigurePermissions("delete:workflow-definitions");
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var count = await _workflowDefinitionManager.BulkDeleteByIdsAsync(request.Ids, cancellationToken);
        return new Response(count);
    }
}