using System.IO.Compression;
using Elsa.Abstractions;
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

    /// <inheritdoc />
    public ImportFiles(
        IWorkflowDefinitionStore workflowDefinitionStore,
        IWorkflowDefinitionImporter workflowDefinitionImporter,
        IApiSerializer apiSerializer,
        IAuthorizationService authorizationService)
    {
        _workflowDefinitionStore = workflowDefinitionStore;
        _workflowDefinitionImporter = workflowDefinitionImporter;
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
        if (Files.Any())
        {
            var models = await ReadWorkflowDefinitionModelsAsync(Files, cancellationToken);
            var authorizationResult = await _authorizationService.AuthorizeWorkflowDefinitionImportsAsync(User, _workflowDefinitionStore, models, cancellationToken);

            if (!authorizationResult.Succeeded)
            {
                await Send.ForbiddenAsync(cancellationToken);
                return;
            }

            var count = await ImportWorkflowDefinitionsAsync(models, cancellationToken);

            if (!ValidationFailed)
                await Send.OkAsync(new { Count = count }, cancellationToken);
        }

        if (ValidationFailed)
            await Send.ErrorsAsync(400, cancellationToken);
    }

    private async Task<ICollection<WorkflowDefinitionModel>> ReadWorkflowDefinitionModelsAsync(IFormFileCollection files, CancellationToken cancellationToken)
    {
        var models = new List<WorkflowDefinitionModel>();

        foreach (var file in files)
        {
            var fileStream = file.OpenReadStream();

            // Check if the file is a JSON file or a ZIP file.
            var isJsonFile = file.ContentType == "application/json";

            // If the file is a JSON file, read it.
            if (isJsonFile)
            {
                models.Add(await ReadJsonStreamAsync(fileStream, cancellationToken));
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
                    models.Add(await ReadJsonStreamAsync(jsonStream, cancellationToken));
                }
            }
        }

        return models;
    }

    private async Task<WorkflowDefinitionModel> ReadJsonStreamAsync(Stream jsonStream, CancellationToken cancellationToken)
    {
        var json = await new StreamReader(jsonStream).ReadToEndAsync(cancellationToken);
        return _apiSerializer.Deserialize<WorkflowDefinitionModel>(json);
    }

    private async Task<int> ImportWorkflowDefinitionsAsync(IEnumerable<WorkflowDefinitionModel> models, CancellationToken cancellationToken)
    {
        var count = 0;

        foreach (var model in models)
        {
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
