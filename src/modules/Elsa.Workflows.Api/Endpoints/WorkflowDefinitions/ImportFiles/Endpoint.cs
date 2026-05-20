using Elsa.Abstractions;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Api.Security;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Mappers;
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
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowDefinitionImporter _workflowDefinitionImporter;
    private readonly WorkflowDefinitionMapper _workflowDefinitionMapper;
    private readonly IApiSerializer _apiSerializer;
    private readonly IAuthorizationService _authorizationService;
    private readonly WorkflowDefinitionScriptAuthorizationService _scriptAuthorizationService;

    /// <inheritdoc />
    public ImportFiles(
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowDefinitionImporter workflowDefinitionImporter,
        WorkflowDefinitionMapper workflowDefinitionMapper,
        IApiSerializer apiSerializer,
        IAuthorizationService authorizationService,
        WorkflowDefinitionScriptAuthorizationService scriptAuthorizationService)
    {
        _workflowDefinitionService = workflowDefinitionService;
        _workflowDefinitionImporter = workflowDefinitionImporter;
        _workflowDefinitionMapper = workflowDefinitionMapper;
        _apiSerializer = apiSerializer;
        _authorizationService = authorizationService;
        _scriptAuthorizationService = scriptAuthorizationService;
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
        var authorizationResult = await _authorizationService.AuthorizeAsync(User, new NotReadOnlyResource(), AuthorizationPolicies.NotReadOnlyPolicy);

        if (!authorizationResult.Succeeded)
        {
            await Send.ForbiddenAsync(cancellationToken);
            return;
        }

        if (Files.Any())
        {
            var count = await ImportFilesAsync(Files, cancellationToken);

            if (!ValidationFailed && !HttpContext.Response.HasStarted)
                await Send.OkAsync(new { Count = count }, cancellationToken);
        }

        if (ValidationFailed && !HttpContext.Response.HasStarted)
            await Send.ErrorsAsync(400, cancellationToken);
    }

    private async Task<int> ImportFilesAsync(IFormFileCollection files, CancellationToken cancellationToken)
    {
        var models = await WorkflowDefinitionImportFileReader.ReadAsync(files, _apiSerializer, () => HttpContext.Response.HasStarted, cancellationToken);

        foreach (var model in models)
        {
            var scriptAuthorizationResult = await _scriptAuthorizationService.AuthorizeAsync(model, User, cancellationToken);
            if (!scriptAuthorizationResult.Succeeded)
            {
                await WorkflowDefinitionScriptAuthorizationFailure.SendAsync(scriptAuthorizationResult, Send.ForbiddenAsync, message => AddError(message), Send.ErrorsAsync, cancellationToken);
                return 0;
            }
        }

        var count = 0;
        foreach (var model in models)
        {
            if (HttpContext.Response.HasStarted)
                return count;

            await ImportSingleWorkflowDefinitionAsync(model, cancellationToken);
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
