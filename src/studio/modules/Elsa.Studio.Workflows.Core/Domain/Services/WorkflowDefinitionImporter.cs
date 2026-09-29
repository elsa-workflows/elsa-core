using System.IO.Compression;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Api.Client.Converters;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Notifications;
using Microsoft.AspNetCore.Components.Forms;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// A workflow definition service that uses a remote backend to retrieve workflow definitions.
/// </summary>
public class WorkflowDefinitionImporter(IBackendApiClientProvider backendApiClientProvider, IMediator mediator, IWorkflowJsonDetector workflowJsonDetector) : IWorkflowDefinitionImporter
{
    private async Task<IWorkflowDefinitionsApi> GetApiAsync(CancellationToken cancellationToken = default)
    {
        return await backendApiClientProvider.GetApiAsync<IWorkflowDefinitionsApi>(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowImportResult>> ImportFilesAsync(IReadOnlyList<IBrowserFile> files, ImportOptions? options = null)
    {
        var maxAllowedSize = options?.MaxAllowedSize ?? 1024 * 1024 * 10; // 10 MB
        var results = new List<WorkflowImportResult>();

        foreach (var file in files)
        {
            await mediator.NotifyAsync(new ImportingFile(file));
            await using var stream = file.OpenReadStream(maxAllowedSize);

            if (file.ContentType == MediaTypeNames.Application.Zip || file.Name.EndsWith(".zip"))
            {
                var importZipFileResults = await ImportZipFileAsync(stream, options);
                results.AddRange(importZipFileResults);
                await mediator.NotifyAsync(new ImportedFile(file));
            }

            else if (file.ContentType == MediaTypeNames.Application.Json || file.Name.EndsWith(".json"))
            {
                var importFromStreamResult = await ImportFromStreamAsync(file.Name, stream, options);
                results.Add(importFromStreamResult);
                await mediator.NotifyAsync(new ImportedFile(file));
            }
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IList<WorkflowImportResult>> ImportZipFileAsync(Stream stream, ImportOptions? options)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);
        var importResultList = new List<WorkflowImportResult>();

        try
        {
            var zipArchive = new ZipArchive(memoryStream);
            foreach (var entry in zipArchive.Entries.Where(x => !x.FullName.StartsWith("__MACOSX", StringComparison.OrdinalIgnoreCase)))
            {
                if (entry.FullName.EndsWith(".json"))
                {
                    await using var entryStream = entry.Open();
                    var result = await ImportFromStreamAsync(entry.Name, entryStream, options);

                    importResultList.Add(result);
                }
                else if (entry.FullName.EndsWith(".zip"))
                {
                    await using var entryStream = entry.Open();
                    var results = await ImportZipFileAsync(entryStream, options);
                    importResultList.AddRange(results);
                }
            }
        }
        catch (Exception e)
        {
            if (options?.ErrorCallback != null)
                await options.ErrorCallback(e);
            importResultList.Add(new()
            {
                Failure = new(e.Message, WorkflowImportFailureType.Exception)
            });
        }

        return importResultList;
    }

    /// <inheritdoc />
    public async Task<WorkflowImportResult> ImportFromStreamAsync(string fileName, Stream stream, ImportOptions? options)
    {
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        return await ImportJsonAsync(fileName, json, options);
    }

    /// <inheritdoc />
    public async Task<WorkflowImportResult> ImportJsonAsync(string fileName, string json, ImportOptions? options)
    {
        var jsonSerializerOptions = CreateJsonSerializerOptions();

        try
        {
            await mediator.NotifyAsync(new ImportingJson(json));

            if (!workflowJsonDetector.IsWorkflowSchema(json))
            {
                return new()
                {
                    FileName = fileName,
                    Failure = new("Invalid schema", WorkflowImportFailureType.InvalidSchema)
                };
            }

            var model = JsonSerializer.Deserialize<WorkflowDefinitionModel>(json, jsonSerializerOptions)!;

            if (options?.DefinitionId != null)
                model.DefinitionId = options.DefinitionId;

            await mediator.NotifyAsync(new ImportingWorkflowDefinition(model));
            var api = await GetApiAsync();
            var newWorkflowDefinition = await api.ImportAsync(model);

            if (options?.ImportedCallback != null)
                await options.ImportedCallback(newWorkflowDefinition);

            await mediator.NotifyAsync(new ImportedWorkflowDefinition(newWorkflowDefinition));
            await mediator.NotifyAsync(new ImportedJson(json));

            return new()
            {
                FileName = fileName,
                WorkflowDefinition = newWorkflowDefinition
            };
        }
        catch (Exception e)
        {
            if (options?.ErrorCallback != null)
                await options.ErrorCallback(e);
            return new()
            {
                FileName = fileName,
                Failure = new(e.Message, WorkflowImportFailureType.Exception)
            };
        }
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new VersionOptionsJsonConverter());
        return options;
    }
}