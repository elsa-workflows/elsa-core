using Elsa.Abstractions;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Import;

/// <summary>
/// Imports a JSON file containing a workflow definition.
/// </summary>
[PublicAPI]
internal class Import : ElsaEndpoint<WorkflowDefinitionModel, WorkflowDefinitionModel>
{
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowDefinitionImporter _workflowDefinitionImporter;
    private readonly WorkflowDefinitionMapper _workflowDefinitionMapper;

    /// <inheritdoc />
    public Import(
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowDefinitionImporter workflowDefinitionImporter,
        WorkflowDefinitionMapper workflowDefinitionMapper)
    {
        _workflowDefinitionService = workflowDefinitionService;
        _workflowDefinitionImporter = workflowDefinitionImporter;
        _workflowDefinitionMapper = workflowDefinitionMapper;
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

        // Map the workflow definition for serialization.
        var updatedModel = await _workflowDefinitionMapper.MapAsync(draft, cancellationToken);

        if (isNew)
            await SendCreatedAtAsync<Get.Get>(new { DefinitionId = definitionId }, updatedModel, cancellation: cancellationToken);
        else
            await SendOkAsync(updatedModel, cancellationToken);
    }
}