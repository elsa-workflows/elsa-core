using Elsa.Abstractions;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Import;

/// <summary>
/// Imports a JSON file containing a workflow definition.
/// </summary>
[PublicAPI]
internal class Import : ElsaEndpoint<WorkflowDefinitionModel, WorkflowDefinitionResponse>
{
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowDefinitionImporter _workflowDefinitionImporter;

    /// <inheritdoc />
    public Import(
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowDefinitionImporter workflowDefinitionImporter)
    {
        _workflowDefinitionService = workflowDefinitionService;
        _workflowDefinitionImporter = workflowDefinitionImporter;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Routes("workflow-definitions/import", "workflow-definitions/{definitionId}/import");
        Verbs(FastEndpoints.Http.POST, FastEndpoints.Http.PUT);
        ConfigurePermissions("write:workflow-definitions");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(WorkflowDefinitionModel model, CancellationToken cancellationToken)
    {
        var definitionId = model.DefinitionId;
        var isNew = string.IsNullOrWhiteSpace(definitionId);

        // Import workflow
        var saveWorkflowRequest = new SaveWorkflowDefinitionRequest
        {
            Model = model,
            Publish = false,
        };
        var draft = await _workflowDefinitionImporter.ImportAsync(saveWorkflowRequest, cancellationToken);

        // Materialize the workflow definition for serialization.
        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(draft, cancellationToken);

        var response = new WorkflowDefinitionResponse(
            draft.Id,
            draft.DefinitionId,
            draft.Name,
            draft.Description,
            draft.CreatedAt,
            draft.Version,
            model.Variables ?? new List<VariableDefinition>(),
            draft.Inputs,
            draft.Outputs,
            draft.Outcomes,
            draft.CustomProperties,
            draft.IsLatest,
            draft.IsPublished,
            draft.UsableAsActivity,
            workflow.Root,
            draft.Options);

        if (isNew)
            await SendCreatedAtAsync<Get.Get>(new { DefinitionId = definitionId }, response, cancellation: cancellationToken);
        else
            await SendOkAsync(response, cancellationToken);
    }
}