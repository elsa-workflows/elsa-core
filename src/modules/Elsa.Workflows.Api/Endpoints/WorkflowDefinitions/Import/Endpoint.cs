using Elsa.Abstractions;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Services;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Import;

/// <summary>
/// Imports JSON and/or ZIP files containing a workflow definitions.
/// </summary>
[PublicAPI]
internal class Import : ElsaEndpoint<WorkflowDefinitionModel>
{
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowDefinitionImporter _workflowDefinitionImporter;
    private readonly WorkflowDefinitionMapper _workflowDefinitionMapper;
    private readonly IApiSerializer _apiSerializer;
    private readonly IWorkflowDefinitionLinkService _linkService;

    /// <inheritdoc />
    public Import(
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowDefinitionImporter workflowDefinitionImporter,
        WorkflowDefinitionMapper workflowDefinitionMapper,
        IApiSerializer apiSerializer,
        IWorkflowDefinitionLinkService linkService)
    {
        _workflowDefinitionService = workflowDefinitionService;
        _workflowDefinitionImporter = workflowDefinitionImporter;
        _workflowDefinitionMapper = workflowDefinitionMapper;
        _apiSerializer = apiSerializer;
        _linkService = linkService;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Routes("workflow-definitions/import", "workflow-definitions/{definitionId}/import");
        Verbs(FastEndpoints.Http.POST, FastEndpoints.Http.PUT);
        ConfigurePermissions("write:workflow-definitions");
        Policies(AuthorizationPolicies.NotReadOnlyPolicy);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(WorkflowDefinitionModel model, CancellationToken cancellationToken)
    {
        var definitionId = model.DefinitionId;
        var isNew = string.IsNullOrWhiteSpace(definitionId);
        var result = await ImportSingleWorkflowDefinitionAsync(model, cancellationToken);
        var definition = result.WorkflowDefinition;
        var updatedModel = await _workflowDefinitionMapper.MapAsync(definition, cancellationToken);
        updatedModel = _linkService.GenerateLinksForSingleEntry(updatedModel);

        if (result.Succeeded)
        {
            if (isNew)
                await SendCreatedAtAsync<GetByDefinitionId.GetByDefinitionId>(new { DefinitionId = definitionId }, updatedModel, cancellation: cancellationToken);
            else
                await SendOkAsync(updatedModel, cancellationToken);
        }

        if (ValidationFailed)
            await SendErrorsAsync(400, cancellationToken);
    }

    private async Task<ImportWorkflowResult> ImportSingleWorkflowDefinitionAsync(WorkflowDefinitionModel model, CancellationToken cancellationToken)
    {
        // Import workflow
        var saveWorkflowRequest = new SaveWorkflowDefinitionRequest
        {
            Model = model,
            Publish = false,
        };

        var result = await _workflowDefinitionImporter.ImportAsync(saveWorkflowRequest, cancellationToken);

        if (result.Succeeded)
            return result;

        foreach (var validationError in result.ValidationErrors)
            AddError(validationError.Message);

        return result;
    }
}