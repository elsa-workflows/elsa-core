using System.IO.Compression;
using Elsa.Abstractions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

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
    private readonly IApiSerializer _apiSerializer;

    /// <inheritdoc />
    public Import(
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowDefinitionImporter workflowDefinitionImporter,
        WorkflowDefinitionMapper workflowDefinitionMapper,
        IApiSerializer apiSerializer)
    {
        _workflowDefinitionService = workflowDefinitionService;
        _workflowDefinitionImporter = workflowDefinitionImporter;
        _workflowDefinitionMapper = workflowDefinitionMapper;
        _apiSerializer = apiSerializer;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Routes("workflow-definitions/import", "workflow-definitions/{definitionId}/import");
        Verbs(FastEndpoints.Http.POST, FastEndpoints.Http.PUT);
        ConfigurePermissions("write:workflow-definitions");
        AllowFileUploads();
    }

    /// <inheritdoc />
    public override async Task HandleAsync(WorkflowDefinitionModel model, CancellationToken cancellationToken)
    {
        if (Files.Any())
            await ImportFilesAsync(Files, cancellationToken);
        else
        {
            var definitionId = model.DefinitionId;
            var isNew = string.IsNullOrWhiteSpace(definitionId);
            var result = await ImportSingleWorkflowDefinitionAsync(model, cancellationToken);
            var definition = result.WorkflowDefinition;
            var updatedModel = await _workflowDefinitionMapper.MapAsync(definition, cancellationToken);

            if (result.Succeeded)
            {
                if (isNew)
                    await SendCreatedAtAsync<GetByDefinitionId.GetByDefinitionId>(new { DefinitionId = definitionId }, updatedModel, cancellation: cancellationToken);
                else
                    await SendOkAsync(updatedModel, cancellationToken);
            }
        }

        if (ValidationFailed)
            await SendErrorsAsync(400, cancellationToken);
    }

    private async Task ImportFilesAsync(IFormFileCollection files, CancellationToken cancellationToken)
    {
        foreach (var file in files)
        {
            var fileStream = file.OpenReadStream();

            // Check if the file is a JSON file or a ZIP file.
            var isJsonFile = file.ContentType == "application/json";

            // If the file is a JSON file, read it.
            if (isJsonFile)
            {
                await ImportJsonStreamAsync(fileStream, cancellationToken);
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
                }
            }
        }
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