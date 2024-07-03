using Elsa.Abstractions;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Refresh;

[PublicAPI]
internal class Refresh(IWorkflowDefinitionsRefresher workflowDefinitionsRefresher) : ElsaEndpoint<Request>
{
    private const int BatchSize = 10;

    public override void Configure()
    {
        Post("/actions/workflow-definitions/refresh");
        ConfigurePermissions("actions:workflow-definitions:refresh");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        await RefreshWorkflowDefinitionsAsync(request.DefinitionIds, cancellationToken);
        await SendOkAsync(cancellationToken);
    }

    private async Task RefreshWorkflowDefinitionsAsync(ICollection<string>? definitionIds, CancellationToken cancellationToken)
    {
        var request = new RefreshWorkflowDefinitionsRequest(definitionIds, BatchSize);
        await workflowDefinitionsRefresher.RefreshWorkflowDefinitionsAsync(request, cancellationToken);
    }
}