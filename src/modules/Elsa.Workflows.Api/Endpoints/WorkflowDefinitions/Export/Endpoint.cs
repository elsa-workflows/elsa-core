using System.IO.Compression;
using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Models;
using Humanizer;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Export;

/// <summary>
/// Exports the specified workflow definition as JSON download.
/// </summary>
[UsedImplicitly]
internal class Export : ElsaEndpoint<Request>
{
    private readonly IApiSerializer _serializer;
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowReferenceGraphBuilder _workflowReferenceGraphBuilder;
    private readonly WorkflowDefinitionMapper _workflowDefinitionMapper;

    /// <inheritdoc />
    public Export(
        IWorkflowDefinitionStore store,
        IApiSerializer serializer,
        WorkflowDefinitionMapper workflowDefinitionMapper,
        IWorkflowReferenceGraphBuilder workflowReferenceGraphBuilder)
    {
        _store = store;
        _serializer = serializer;
        _workflowDefinitionMapper = workflowDefinitionMapper;
        _workflowReferenceGraphBuilder = workflowReferenceGraphBuilder;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Routes("/bulk-actions/export/workflow-definitions", "/workflow-definitions/{definitionId}/export");
        Verbs(FastEndpoints.Http.GET, FastEndpoints.Http.POST);
        ConfigurePermissions("read:workflow-definitions");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        if (request.DefinitionId != null)
            await DownloadSingleWorkflowAsync(request.DefinitionId, request.VersionOptions, request.IncludeConsumingWorkflows, cancellationToken);
        else if (request.Ids != null)
            await DownloadMultipleWorkflowsAsync(request.Ids, request.IncludeConsumingWorkflows, cancellationToken);
        else await Send.NoContentAsync(cancellationToken);
    }

    private async Task DownloadMultipleWorkflowsAsync(ICollection<string> ids, bool includeConsumingWorkflows, CancellationToken cancellationToken)
    {
        var definitions = (await _store.FindManyAsync(new()
        {
            Ids = ids
        }, cancellationToken)).ToList();

        if (includeConsumingWorkflows)
            definitions = await IncludeConsumersAsync(definitions, cancellationToken);

        if (!definitions.Any())
        {
            await Send.NoContentAsync(cancellationToken);
            return;
        }

        await WriteZipResponseAsync(definitions, cancellationToken);
    }

    private async Task DownloadSingleWorkflowAsync(string definitionId, string? versionOptions, bool includeConsumingWorkflows, CancellationToken cancellationToken)
    {
        var parsedVersionOptions = string.IsNullOrEmpty(versionOptions) ? VersionOptions.Latest : VersionOptions.FromString(versionOptions);
        var definition = (await _store.FindManyAsync(new()
        {
            DefinitionId = definitionId,
            VersionOptions = parsedVersionOptions
        }, cancellationToken)).FirstOrDefault();

        if (definition == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        if (includeConsumingWorkflows)
        {
            var definitions = await IncludeConsumersAsync([definition], cancellationToken);
            await WriteZipResponseAsync(definitions, cancellationToken);
            return;
        }

        var model = await CreateWorkflowModelAsync(definition, cancellationToken);
        var binaryJson = await SerializeWorkflowDefinitionAsync(model, cancellationToken);
        var fileName = GetFileName(model);

        await Send.BytesAsync(binaryJson, fileName, cancellation: cancellationToken);
    }

    /// <summary>
    /// Recursively discovers all consuming workflow definitions and includes them.
    /// Consumers are always resolved at <see cref="VersionOptions.Latest"/>, regardless of the version used for the initial definitions.
    /// </summary>
    private async Task<List<WorkflowDefinition>> IncludeConsumersAsync(List<WorkflowDefinition> definitions, CancellationToken cancellationToken)
    {
        var initialDefinitionIds = definitions.Select(d => d.DefinitionId).ToList();
        var graph = await _workflowReferenceGraphBuilder.BuildGraphAsync(initialDefinitionIds, cancellationToken);
        
        // Find any consumer definitions not already in our list.
        var newDefinitionIds = graph.ConsumerDefinitionIds.Except(initialDefinitionIds).ToList();

        if (newDefinitionIds.Count > 0)
        {
            var consumerDefinitions = await _store.FindManyAsync(new WorkflowDefinitionFilter
            {
                DefinitionIds = newDefinitionIds.ToArray(),
                VersionOptions = VersionOptions.Latest
            }, cancellationToken);

            definitions = definitions.Concat(consumerDefinitions).ToList();
        }

        return definitions;
    }

    private async Task WriteZipResponseAsync(List<WorkflowDefinition> definitions, CancellationToken cancellationToken)
    {
        var zipStream = new MemoryStream();
        
#if NET10_0_OR_GREATER
        await using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            // Create a JSON file for each workflow definition:
            foreach (var definition in definitions)
            {
                var model = await CreateWorkflowModelAsync(definition, cancellationToken);
                var binaryJson = await SerializeWorkflowDefinitionAsync(model, cancellationToken);
                var fileName = GetFileName(model);
                var entry = zipArchive.CreateEntry(fileName, CompressionLevel.Optimal);
                await using var entryStream = await entry.OpenAsync(cancellationToken);
                await entryStream.WriteAsync(binaryJson, cancellationToken);
            }
        }
#else
        using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            // Create a JSON file for each workflow definition:
            foreach (var definition in definitions)
            {
                var model = await CreateWorkflowModelAsync(definition, cancellationToken);
                var binaryJson = await SerializeWorkflowDefinitionAsync(model, cancellationToken);
                var fileName = GetFileName(model);
                var entry = zipArchive.CreateEntry(fileName, CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await entryStream.WriteAsync(binaryJson, cancellationToken);
            }
        }
#endif
        
        // Send the zip file to the client:
        zipStream.Position = 0;
        await Send.BytesAsync(zipStream.ToArray(), "workflow-definitions.zip", cancellation: cancellationToken);
    }

    private string GetFileName(WorkflowDefinitionModel definition)
    {
        var hasWorkflowName = !string.IsNullOrWhiteSpace(definition.Name);
        var workflowName = hasWorkflowName ? definition.Name!.Trim() : definition.DefinitionId;
        var fileName = $"workflow-definition-{workflowName.Underscore().Dasherize().ToLowerInvariant()}.json";
        return fileName;
    }

    private async Task<byte[]> SerializeWorkflowDefinitionAsync(WorkflowDefinitionModel model, CancellationToken cancellationToken)
    {
        var serializerOptions = _serializer.GetOptions();
        var document = JsonSerializer.SerializeToDocument(model, serializerOptions);
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

    private async Task<WorkflowDefinitionModel> CreateWorkflowModelAsync(WorkflowDefinition definition, CancellationToken cancellationToken)
    {
        return await _workflowDefinitionMapper.MapAsync(definition, cancellationToken);
    }
}