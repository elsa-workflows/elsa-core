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
    private readonly WorkflowDefinitionMapper _workflowDefinitionMapper;
    private readonly IWorkflowReferenceQuery _workflowReferenceQuery;

    /// <inheritdoc />
    public Export(
        IWorkflowDefinitionStore store,
        IWorkflowDefinitionService workflowDefinitionService,
        IApiSerializer serializer,
        WorkflowDefinitionMapper workflowDefinitionMapper,
        VariableDefinitionMapper variableDefinitionMapper,
        IWorkflowReferenceQuery workflowReferenceQuery)
    {
        _store = store;
        _serializer = serializer;
        _workflowDefinitionMapper = workflowDefinitionMapper;
        _workflowReferenceQuery = workflowReferenceQuery;
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
            await DownloadSingleWorkflowAsync(request.DefinitionId, request.VersionOptions, request.IncludeConsumers, cancellationToken);
        else if (request.Ids != null)
            await DownloadMultipleWorkflowsAsync(request.Ids, request.IncludeConsumers, cancellationToken);
        else await Send.NoContentAsync(cancellationToken);
    }

    private async Task DownloadMultipleWorkflowsAsync(ICollection<string> ids, bool includeConsumers, CancellationToken cancellationToken)
    {
        // Get the initial set of definitions
        List<WorkflowDefinition> definitions = (await _store.FindManyAsync(new()
        {
            Ids = ids
        }, cancellationToken)).ToList();

        if (!definitions.Any())
        {
            await Send.NoContentAsync(cancellationToken);
            return;
        }

        // If includeConsumers is true, add all consuming workflows
        if (includeConsumers)
        {
        if (includeConsumers)
        {
            var allDefinitionIds = new HashSet<string>(definitions.Select(d => d.DefinitionId));
            
            foreach (var definition in definitions)
            {
                var consumingIds = await GetAllConsumingWorkflowDefinitionIdsAsync(definition.DefinitionId, cancellationToken);
                allDefinitionIds.UnionWith(consumingIds);
            }
            
            // Fetch all definitions including consumers (latest versions)
            var allDefinitions = await _store.FindManyAsync(new()
            {
                DefinitionIds = allDefinitionIds.ToArray(),
                VersionOptions = VersionOptions.Latest
            }, cancellationToken);
            
            definitions = allDefinitions.ToList();
        }
        }

        var zipStream = new MemoryStream();
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

        // Send the zip file to the client:
        zipStream.Position = 0;
        await Send.BytesAsync(zipStream.ToArray(), "workflow-definitions.zip", cancellation: cancellationToken);
    }

    private async Task DownloadSingleWorkflowAsync(string definitionId, string? versionOptions, bool includeConsumers, CancellationToken cancellationToken)
    {
        var parsedVersionOptions = string.IsNullOrEmpty(versionOptions) ? VersionOptions.Latest : VersionOptions.FromString(versionOptions);
        WorkflowDefinition? definition = (await _store.FindManyAsync(new()
        {
            DefinitionId = definitionId,
            VersionOptions = parsedVersionOptions
        }, cancellationToken)).FirstOrDefault();

        if (definition == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        // If we need to include consumers, export as a zip with multiple files
        if (includeConsumers)
        {
            var allDefinitionIds = new HashSet<string> { definition.DefinitionId };
            var consumingIds = await GetAllConsumingWorkflowDefinitionIdsAsync(definition.DefinitionId, cancellationToken);
            allDefinitionIds.UnionWith(consumingIds);
            
            // Fetch all definitions including consumers (latest versions)
            var allDefinitions = await _store.FindManyAsync(new()
            {
                DefinitionIds = allDefinitionIds.ToArray(),
                VersionOptions = VersionOptions.Latest
            }, cancellationToken);
            
            await DownloadMultipleWorkflowsAsZipAsync(allDefinitions.ToList(), cancellationToken);
        }
        else
        {
            // Single file export
            var model = await CreateWorkflowModelAsync(definition, cancellationToken);
            var binaryJson = await SerializeWorkflowDefinitionAsync(model, cancellationToken);
            var fileName = GetFileName(model);

            await Send.BytesAsync(binaryJson, fileName, cancellation: cancellationToken);
        }
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
        var binaryJson = output.ToArray();
        return binaryJson;
    }

    private async Task<WorkflowDefinitionModel> CreateWorkflowModelAsync(WorkflowDefinition definition, CancellationToken cancellationToken)
    {
        return await _workflowDefinitionMapper.MapAsync(definition, cancellationToken);
    }

    private async Task DownloadMultipleWorkflowsAsZipAsync(List<WorkflowDefinition> definitions, CancellationToken cancellationToken)
    {
        var zipStream = new MemoryStream();
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

        // Send the zip file to the client:
        zipStream.Position = 0;
        await Send.BytesAsync(zipStream.ToArray(), "workflow-definitions.zip", cancellation: cancellationToken);
    }

    private async Task<IEnumerable<string>> GetAllConsumingWorkflowDefinitionIdsAsync(
        string definitionId,
        CancellationToken cancellationToken,
        HashSet<string>? visitedIds = null)
    {
        visitedIds ??= new HashSet<string>();
        var allConsumingIds = new List<string>();

        // If we've already processed this definition ID, skip it to prevent infinite recursion.
        if (!visitedIds.Add(definitionId))
            return allConsumingIds;

        // Get direct references
        var directRefs = await _workflowReferenceQuery.ExecuteAsync(definitionId, cancellationToken);
        allConsumingIds.AddRange(directRefs);

        // Recursively get consumers of consumers
        foreach (var refId in directRefs)
        {
            var transitiveRefs = await GetAllConsumingWorkflowDefinitionIdsAsync(refId, cancellationToken, visitedIds);
            allConsumingIds.AddRange(transitiveRefs);
        }

        return allConsumingIds.Distinct();
    }
}