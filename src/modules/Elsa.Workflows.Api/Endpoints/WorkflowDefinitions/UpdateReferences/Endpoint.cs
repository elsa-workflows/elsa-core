using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Management.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.UpdateReferences;

[PublicAPI]
internal class UpdateReferences : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowDefinitionManager _workflowDefinitionManager;

    public UpdateReferences(IWorkflowDefinitionStore store, IWorkflowDefinitionManager workflowDefinitionManager)
    {
        _store = store;
        _workflowDefinitionManager = workflowDefinitionManager;
    }

    public override void Configure()
    {
        Post("/workflow-definitions/{definitionId}/update-references");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowDefinitionFilter
        {
            DefinitionId = request.DefinitionId,
            VersionOptions = VersionOptions.Latest
        };

        var definition = await _store.FindAsync(filter, cancellationToken);

        if (definition == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var affectedWorkflows = await _workflowDefinitionManager.UpdateReferencesInConsumingWorkflows(definition, cancellationToken);

        var response = new Response { AffectedWorkflows = affectedWorkflows.Select(w => w.Name ?? w.DefinitionId) };
        await SendOkAsync(response, cancellationToken);
    }
}