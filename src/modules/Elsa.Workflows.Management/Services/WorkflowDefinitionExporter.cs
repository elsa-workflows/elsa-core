using System.IO.Compression;
using System.Text.Json;
using Elsa.Common.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Models;
using Humanizer;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowDefinitionExporter(
    IWorkflowDefinitionStore store,
    IApiSerializer serializer,
    WorkflowDefinitionMapper workflowDefinitionMapper,
    IWorkflowReferenceGraphBuilder workflowReferenceGraphBuilder) : IWorkflowDefinitionExporter
{
    /// <inheritdoc />
    public async Task<WorkflowDefinitionExportResult?> ExportAsync(string definitionId, VersionOptions? versionOptions = null, bool includeConsumingWorkflows = false, CancellationToken cancellationToken = default)
    {
        var parsedVersionOptions = versionOptions ?? VersionOptions.Latest;
        var definition = (await store.FindManyAsync(new()
        {
            DefinitionId = definitionId,
            VersionOptions = parsedVersionOptions
        }, cancellationToken)).FirstOrDefault();

        if (definition == null)
            return null;

        if (includeConsumingWorkflows)
        {
            var definitions = await IncludeConsumersAsync([definition], cancellationToken);
            return await ExportDefinitionsAsync(definitions, cancellationToken);
        }

        return await ExportDefinitionAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinitionExportResult?> ExportManyAsync(ICollection<string> ids, bool includeConsumingWorkflows = false, CancellationToken cancellationToken = default)
    {
        var definitions = (await store.FindManyAsync(new()
        {
            Ids = ids
        }, cancellationToken)).ToList();

        if (includeConsumingWorkflows)
            definitions = await IncludeConsumersAsync(definitions, cancellationToken);

        if (definitions.Count == 0)
            return null;

        return await ExportDefinitionsAsync(definitions, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinitionExportResult> ExportDefinitionAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var model = await workflowDefinitionMapper.MapAsync(definition, cancellationToken);
        var binaryJson = await SerializeWorkflowDefinitionAsync(model, cancellationToken);
        var fileName = GetFileName(model);
        return new(binaryJson, fileName);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinitionExportResult> ExportDefinitionsAsync(ICollection<WorkflowDefinition> definitions, CancellationToken cancellationToken = default)
    {
        var zipBytes = await CreateZipArchiveAsync(definitions, cancellationToken);
        return new(zipBytes, "workflow-definitions.zip");
    }

    /// <summary>
    /// Recursively discovers all consuming workflow definitions and includes them.
    /// Consumers are always resolved at <see cref="VersionOptions.Latest"/>, regardless of the version used for the initial definitions.
    /// </summary>
    private async Task<List<WorkflowDefinition>> IncludeConsumersAsync(List<WorkflowDefinition> definitions, CancellationToken cancellationToken)
    {
        var initialDefinitionIds = definitions.Select(d => d.DefinitionId).ToList();
        var graph = await workflowReferenceGraphBuilder.BuildGraphAsync(initialDefinitionIds, cancellationToken);

        // Find any consumer definitions not already in our list.
        var newDefinitionIds = graph.ConsumerDefinitionIds.Except(initialDefinitionIds).ToList();

        if (newDefinitionIds.Count > 0)
        {
            var consumerDefinitions = await store.FindManyAsync(new()
            {
                DefinitionIds = newDefinitionIds.ToArray(),
                VersionOptions = VersionOptions.Latest
            }, cancellationToken);

            definitions = definitions.Concat(consumerDefinitions).ToList();
        }

        return definitions;
    }

    private async Task<byte[]> CreateZipArchiveAsync(ICollection<WorkflowDefinition> definitions, CancellationToken cancellationToken)
    {
        var zipStream = new MemoryStream();
        var sortedDefinitions = definitions.OrderBy(d => d.DefinitionId).ToList();

        // NOTE:
        // - ZIP timestamps cannot be earlier than 1980-01-01 (the ZIP format's minimum).
        // - We intentionally use a fixed timestamp (instead of DateTimeOffset.UtcNow) to keep exports deterministic.
        //   This avoids producing different ZIP bytes for identical exports, which helps tests, caching, and diffing.
        var zipEpoch = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

#if NET10_0_OR_GREATER
        await using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            foreach (var definition in sortedDefinitions)
            {
                var model = await workflowDefinitionMapper.MapAsync(definition, cancellationToken);
                var binaryJson = await SerializeWorkflowDefinitionAsync(model, cancellationToken);
                var fileName = GetFileName(model);
                var entry = zipArchive.CreateEntry(fileName, CompressionLevel.Optimal);
                entry.LastWriteTime = zipEpoch;
                await using var entryStream = await entry.OpenAsync(cancellationToken);
                await entryStream.WriteAsync(binaryJson, cancellationToken);
            }
        }
#else
        using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            foreach (var definition in sortedDefinitions)
            {
                var model = await workflowDefinitionMapper.MapAsync(definition, cancellationToken);
                var binaryJson = await SerializeWorkflowDefinitionAsync(model, cancellationToken);
                var fileName = GetFileName(model);
                var entry = zipArchive.CreateEntry(fileName, CompressionLevel.Optimal);
                entry.LastWriteTime = zipEpoch;
                await using var entryStream = entry.Open();
                await entryStream.WriteAsync(binaryJson, cancellationToken);
            }
        }
#endif

        zipStream.Position = 0;
        return zipStream.ToArray();
    }

    private static readonly char[] InvalidFileNameCharacters = Path.GetInvalidFileNameChars();

    private static string GetFileName(WorkflowDefinitionModel definition)
    {
        var hasWorkflowName = !string.IsNullOrWhiteSpace(definition.Name);
        var workflowName = hasWorkflowName ? definition.Name!.Trim() : definition.DefinitionId;
        var workflowSlug = workflowName.Underscore().Dasherize().ToLowerInvariant();
        var dynamicFileNamePart = $"{workflowSlug}-{definition.DefinitionId}";
        var sanitizedDynamicFileNamePart = SanitizeFileName(dynamicFileNamePart);

        return $"workflow-definition-{sanitizedDynamicFileNamePart}.json";
    }

    private static string SanitizeFileName(string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (!IsInvalidFileNameCharacter(value[i]))
                continue;

            return string.Create(value.Length, (value, i), static (buffer, state) =>
            {
                state.value.AsSpan(0, state.i).CopyTo(buffer);

                for (var j = state.i; j < state.value.Length; j++)
                {
                    var character = state.value[j];
                    buffer[j] = IsInvalidFileNameCharacter(character) ? '-' : character;
                }
            });
        }

        return value;
    }

    private static bool IsInvalidFileNameCharacter(char character) => character is '/' or '\\' || Array.IndexOf(InvalidFileNameCharacters, character) >= 0;

    private async Task<byte[]> SerializeWorkflowDefinitionAsync(WorkflowDefinitionModel model, CancellationToken cancellationToken)
    {
        var serializerOptions = serializer.GetOptions();
        using var document = JsonSerializer.SerializeToDocument(model, serializerOptions);
        var rootElement = document.RootElement;

        using var output = new MemoryStream();
        await using var writer = new Utf8JsonWriter(output);

        writer.WriteStartObject();
        writer.WriteString("$schema", "https://elsaworkflows.io/schemas/workflow-definition/v3.0.0/schema.json");

        foreach (var property in rootElement.EnumerateObject())
            property.WriteTo(writer);

        writer.WriteEndObject();

        await writer.FlushAsync(cancellationToken);
        return output.ToArray();
    }
}

