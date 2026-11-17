using Elsa.Abstractions;
using Elsa.Authorization;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.ValidateVersion;

internal sealed class ValidateVersion(
    IWorkflowDefinitionStore store,
    IWorkflowDefinitionService definitionService,
    IEnumerable<IWorkflowDiagnosticProvider> providers) : ElsaEndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("/workflow-definition-versions/{id}/validation");
        RequirePermission(Elsa.Workflows.Api.Permissions.WorkflowPermissions.Definitions, CoreVerbs.View);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var definition = await store.FindAsync(new WorkflowDefinitionFilter { Id = Route<string>("id")! }, cancellationToken);
        if (definition == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        var registeredProviders = providers.ToArray();
        WorkflowValidationError[] diagnostics = [];
        if (registeredProviders.Length > 0)
        {
            var graph = await definitionService.MaterializeWorkflowAsync(definition, cancellationToken);
            diagnostics = registeredProviders.SelectMany(provider => provider.GetDiagnostics(graph)).ToArray();
        }

        await Send.OkAsync(new Response(definition.DefinitionId, definition.Id, definition.Version, diagnostics), cancellationToken);
    }
}

internal sealed record Response(string DefinitionId, string DefinitionVersionId, int Version, IReadOnlyCollection<WorkflowValidationError> Diagnostics);
