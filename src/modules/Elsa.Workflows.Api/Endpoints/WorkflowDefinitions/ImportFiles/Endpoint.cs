using System.IO.Compression;
using Elsa.Abstractions;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
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

    /// <inheritdoc />
    public ImportFiles(
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowDefinitionImporter workflowDefinitionImporter,
        WorkflowDefinitionMapper workflowDefinitionMapper,
        IApiSerializer apiSerializer,
        IAuthorizationService authorizationService)
    {
        _workflowDefinitionService = workflowDefinitionService;
        _workflowDefinitionImporter = workflowDefinitionImporter;
        _workflowDefinitionMapper = workflowDefinitionMapper;
        _apiSerializer = apiSerializer;
        _authorizationService = authorizationService;
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
        var authorizationResult = _authorizationService.AuthorizeAsync(User, null, AuthorizationPolicies.NotReadOnlyPolicy);

        if (!authorizationResult.Result.Succeeded)
        {
            await SendForbiddenAsync(cancellationToken);
            return;
        }

        if (Files.Any())
        {
            var count = await ImportFilesAsync(Files, cancellationToken);

            if (!ValidationFailed)
                await SendOkAsync(new { Count = count }, cancellationToken);
        }

        if (ValidationFailed)
            await SendErrorsAsync(400, cancellationToken);
    }

    private async Task<int> ImportFilesAsync(IFormFileCollection files, CancellationToken cancellationToken)
    {
        var count = 0;

        foreach (var file in files)
        {
            var fileStream = file.OpenReadStream();

            // Check if the file is a JSON file or a ZIP file.
            var isJsonFile = file.ContentType == "application/json";

            // If the file is a JSON file, read it.
            if (isJsonFile)
            {
                await ImportJsonStreamAsync(fileStream, cancellationToken);
                count++;
            }
            else
            {
                // If the file is a ZIP file, extract the JSON files and read them.
                var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read);

                foreach (var entry in zipArchive.Entries)
                {
                    if (!entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var jsonStream = entry.Open();
                    await ImportJsonStreamAsync(jsonStream, cancellationToken);
                    count++;
                }
            }
        }

        return count;
    }

    private async Task ImportJsonStreamAsync(Stream jsonStream, CancellationToken cancellationToken)
    {
        var json = await new StreamReader(jsonStream).ReadToEndAsync();
        var model = _apiSerializer.Deserialize<WorkflowDefinitionModel>(json);
        await ImportSingleWorkflowDefinitionAsync(model, cancellationToken);
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