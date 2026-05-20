using Elsa.Abstractions;
using Elsa.Workflows.Api.Security;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.ImportFiles;

/// <summary>
/// Imports JSON and/or ZIP files containing a workflow definitions.
/// </summary>
[PublicAPI]
internal class ImportFiles : ElsaEndpoint<WorkflowDefinitionModel>
{
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IWorkflowDefinitionImporter _workflowDefinitionImporter;
    private readonly IApiSerializer _apiSerializer;
    private readonly IAuthorizationService _authorizationService;
    private readonly PythonWorkflowDefinitionAuthorizationService _pythonAuthorizationService;

    /// <inheritdoc />
    public ImportFiles(
        IWorkflowDefinitionStore workflowDefinitionStore,
        IWorkflowDefinitionImporter workflowDefinitionImporter,
        IApiSerializer apiSerializer,
        IAuthorizationService authorizationService,
        PythonWorkflowDefinitionAuthorizationService pythonAuthorizationService)
    {
        _workflowDefinitionStore = workflowDefinitionStore;
        _workflowDefinitionImporter = workflowDefinitionImporter;
        _apiSerializer = apiSerializer;
        _authorizationService = authorizationService;
        _pythonAuthorizationService = pythonAuthorizationService;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("workflow-definitions/import-files");
        ConfigurePermissions("write:workflow-definitions");
        AllowFileUploads();
    }

    /// <inheritdoc />
    public override async Task HandleAsync(WorkflowDefinitionModel model, CancellationToken cancellationToken)
    {
        if (Files.Any())
        {
            var models = await ReadWorkflowDefinitionModelsAsync(Files, cancellationToken);
            if (!await AuthorizePythonUsageAsync(models, cancellationToken))
                return;

            var authorizationResult = await _authorizationService.AuthorizeWorkflowDefinitionImportsAsync(User, _workflowDefinitionStore, models, cancellationToken);

            if (!authorizationResult.Succeeded)
            {
                await Send.ForbiddenAsync(cancellationToken);
                return;
            }

            var count = await ImportWorkflowDefinitionsAsync(models, cancellationToken);

            if (!ValidationFailed && !HttpContext.Response.HasStarted)
                await Send.OkAsync(new { Count = count }, cancellationToken);
        }

        if (ValidationFailed && !HttpContext.Response.HasStarted)
            await Send.ErrorsAsync(400, cancellationToken);
    }

    private async Task<ICollection<WorkflowDefinitionModel>> ReadWorkflowDefinitionModelsAsync(IFormFileCollection files, CancellationToken cancellationToken)
    {
        var models = await WorkflowDefinitionImportFileReader.ReadAsync(files, _apiSerializer, () => HttpContext.Response.HasStarted, cancellationToken);
        return models.ToList();
    }

    private async Task<bool> AuthorizePythonUsageAsync(IEnumerable<WorkflowDefinitionModel> models, CancellationToken cancellationToken)
    {
        foreach (var model in models)
        {
            var pythonAuthorizationResult = await _pythonAuthorizationService.AuthorizeAsync(model, User, cancellationToken);
            if (pythonAuthorizationResult != PythonWorkflowDefinitionAuthorizationResult.Allowed)
            {
                await PythonWorkflowDefinitionAuthorizationFailure.SendAsync(pythonAuthorizationResult, Send.ForbiddenAsync, message => AddError(message), Send.ErrorsAsync, cancellationToken);
                return false;
            }
        }

        return true;
    }

    private async Task<int> ImportWorkflowDefinitionsAsync(IEnumerable<WorkflowDefinitionModel> models, CancellationToken cancellationToken)
    {
        var count = 0;
        foreach (var model in models)
        {
            if (HttpContext.Response.HasStarted)
                return count;

            var result = await ImportSingleWorkflowDefinitionAsync(model, cancellationToken);

            if (result.Succeeded)
                count++;
        }

        return count;
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
