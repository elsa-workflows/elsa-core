using System.IO.Compression;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Api.Client.Converters;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
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
    private const int MaxArchiveDepth = 8;
    private const int MaxArchiveEntries = 1024;

    private async Task<IWorkflowDefinitionsApi> GetApiAsync(CancellationToken cancellationToken = default)
    {
        return await backendApiClientProvider.GetApiAsync<IWorkflowDefinitionsApi>(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowImportResult>> ImportFilesAsync(IReadOnlyList<IBrowserFile> files, ImportOptions? options = null)
    {
        options ??= new ImportOptions();
        var results = new List<WorkflowImportResult>();

        foreach (var file in files)
        {
            await mediator.NotifyAsync(new ImportingFile(file));
            await using var stream = file.OpenReadStream(options.MaxAllowedSize);

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
        options ??= new ImportOptions();
        var results = new List<WorkflowImportResult>();
        var remainingExpandedBytes = (long)options.MaxArchiveExpandedSize;
        var remainingEntries = MaxArchiveEntries;

        try
        {
            using var archiveBuffer = new MemoryStream();
            await CopyWithinLimitAsync(stream, archiveBuffer, options.MaxAllowedSize, "ZIP upload");
            await ImportEntriesAsync(archiveBuffer, 1);
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException)
        {
            if (options.ErrorCallback != null)
                await options.ErrorCallback(exception);

            results.Add(new()
            {
                Failure = new(exception.ToUserMessage(), WorkflowImportFailureType.Exception)
            });
        }

        return results;

        async Task ImportEntriesAsync(Stream archiveStream, int depth)
        {
            if (depth > MaxArchiveDepth)
                throw new InvalidDataException($"ZIP import exceeds the limit of {MaxArchiveDepth} archive levels.");

            using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: true);
            if (archive.Entries.Count > remainingEntries)
                throw new InvalidDataException($"ZIP import exceeds the limit of {MaxArchiveEntries} entries.");

            remainingEntries -= archive.Entries.Count;

            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.StartsWith("__MACOSX", StringComparison.OrdinalIgnoreCase))
                    continue;

                var isJson = entry.FullName.EndsWith(".json", StringComparison.Ordinal);
                var isZip = entry.FullName.EndsWith(".zip", StringComparison.Ordinal);
                if (!isJson && !isZip)
                    continue;

                if (entry.Length > remainingExpandedBytes)
                    throw new InvalidDataException("ZIP import exceeds the expanded size limit.");

                await using var entryStream = entry.Open();
                using var expanded = new MemoryStream();
                await CopyWithinLimitAsync(entryStream, expanded, remainingExpandedBytes, "ZIP expansion");
                remainingExpandedBytes -= expanded.Length;

                if (isZip)
                    await ImportEntriesAsync(expanded, depth + 1);
                else
                    results.Add(await ImportFromStreamAsync(entry.Name, expanded, options));
            }
        }
    }

    private static async Task CopyWithinLimitAsync(Stream source, MemoryStream target, long maxBytes, string operation)
    {
        var buffer = new byte[8192];
        while (true)
        {
            var count = await source.ReadAsync(buffer.AsMemory());
            if (count == 0)
                break;

            if (count > maxBytes - target.Length)
                throw new InvalidDataException($"{operation} exceeds the size limit.");

            await target.WriteAsync(buffer.AsMemory(0, count));
        }

        target.Position = 0;
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
                Failure = new(e.ToUserMessage(), WorkflowImportFailureType.Exception)
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