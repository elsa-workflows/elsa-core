using Elsa.Abstractions;
using Elsa.Workflows.Api.Security;
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
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IWorkflowDefinitionImporter _workflowDefinitionImporter;
    private readonly IWorkflowDefinitionLinker _linker;
    private readonly IAuthorizationService _authorizationService;
    private readonly PythonWorkflowDefinitionAuthorizationService _pythonAuthorizationService;

    /// <inheritdoc />
    public Import(
        IWorkflowDefinitionStore workflowDefinitionStore,
        IWorkflowDefinitionImporter workflowDefinitionImporter,
        IWorkflowDefinitionLinker linker,
        IAuthorizationService authorizationService,
        PythonWorkflowDefinitionAuthorizationService pythonAuthorizationService)
    {
        _workflowDefinitionStore = workflowDefinitionStore;
        _workflowDefinitionImporter = workflowDefinitionImporter;
        _linker = linker;
        _authorizationService = authorizationService;
        _pythonAuthorizationService = pythonAuthorizationService;
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

        var pythonAuthorizationResult = await _pythonAuthorizationService.AuthorizeAsync(model, User, cancellationToken);
        if (pythonAuthorizationResult != PythonWorkflowDefinitionAuthorizationResult.Allowed)
        {
            await PythonWorkflowDefinitionAuthorizationFailure.SendAsync(pythonAuthorizationResult, Send.ForbiddenAsync, message => AddError(message), Send.ErrorsAsync, cancellationToken);
            return;
        }

        var authorizationResult = await _authorizationService.AuthorizeWorkflowDefinitionImportAsync(User, _workflowDefinitionStore, model, cancellationToken);

        if (!authorizationResult.Succeeded)
        {
            await Send.ForbiddenAsync(cancellationToken);
            return;
        }

        var result = await ImportSingleWorkflowDefinitionAsync(model, cancellationToken);
        var definition = result.WorkflowDefinition;
        var updatedModel = await _linker.MapAsync(definition, cancellationToken);

        if (result.Succeeded)
        {
            if (isNew)
                await Send.CreatedAtAsync<GetByDefinitionId.GetByDefinitionId>(new { DefinitionId = definitionId }, updatedModel, cancellation: cancellationToken);
            else
                await Send.OkAsync(updatedModel, cancellationToken);
        }

        if (ValidationFailed)
            await Send.ErrorsAsync(400, cancellationToken);
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
