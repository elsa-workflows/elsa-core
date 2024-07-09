using Elsa.Abstractions;
using Elsa.Workflows.Runtime.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Reload;

[PublicAPI]
internal class Refresh(IWorkflowDefinitionsReloader workflowDefinitionsReloader) : ElsaEndpointWithoutRequest
{
    private const int BatchSize = 10;

    public override void Configure()
    {
        Post("/actions/workflow-definitions/reload");
        ConfigurePermissions("actions:workflow-definitions:reload");
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        await ReloadWorkflowDefinitionsAsync(cancellationToken);
        await SendOkAsync(cancellationToken);
    }

    private async Task ReloadWorkflowDefinitionsAsync(CancellationToken cancellationToken)
    {
        await workflowDefinitionsReloader.ReloadWorkflowDefinitionsAsync(cancellationToken);
    }
}