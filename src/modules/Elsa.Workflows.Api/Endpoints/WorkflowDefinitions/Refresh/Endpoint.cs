using Elsa.Abstractions;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Refresh;

[PublicAPI]
internal class Refresh(IWorkflowDefinitionsRefresher workflowDefinitionsRefresher) : ElsaEndpoint<Request, Response>
{
    private const int BatchSize = 10;

    public override void Configure()
    {
        Post("/actions/workflow-definitions/refresh");
        ConfigurePermissions("actions:workflow-definitions:refresh");
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var result = await RefreshWorkflowDefinitionsAsync(request.DefinitionIds, cancellationToken);
        return new Response(result.Refreshed, result.NotFound);
    }

    private async Task<RefreshWorkflowDefinitionsResponse> RefreshWorkflowDefinitionsAsync(ICollection<string>? definitionIds, CancellationToken cancellationToken)
    {
        var request = new RefreshWorkflowDefinitionsRequest(definitionIds, BatchSize);
        return await workflowDefinitionsRefresher.RefreshWorkflowDefinitionsAsync(request, cancellationToken);
    }
}