using Elsa.Abstractions;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Import;

/// <summary>
/// Imports JSON and/or ZIP files containing a workflow definitions.
/// </summary>
[PublicAPI]
internal class Import : ElsaEndpoint<WorkflowDefinitionModel>
{
    private readonly IWorkflowDefinitionImporter _workflowDefinitionImporter;
    private readonly IWorkflowDefinitionLinker _linker;
    private readonly IAuthorizationService _authorizationService;

    /// <inheritdoc />
    public Import(
        IWorkflowDefinitionImporter workflowDefinitionImporter,
        IWorkflowDefinitionLinker linker,
        IAuthorizationService authorizationService)
    {
        _workflowDefinitionImporter = workflowDefinitionImporter;
        _linker = linker;
        _authorizationService = authorizationService;
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
        var result = await ImportSingleWorkflowDefinitionAsync(model, cancellationToken);
        var definition = result.WorkflowDefinition;

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, new NotReadOnlyResource(definition), AuthorizationPolicies.NotReadOnlyPolicy);

        if (!authorizationResult.Succeeded)
        {
            await SendForbiddenAsync(cancellationToken);
            return;
        }

        var updatedModel = await _linker.MapAsync(definition, cancellationToken);

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