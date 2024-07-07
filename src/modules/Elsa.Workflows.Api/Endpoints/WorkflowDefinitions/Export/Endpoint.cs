using System.IO.Compression;
using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Contracts;
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

    /// <inheritdoc />
    public Export(
        IWorkflowDefinitionStore store,
        IWorkflowDefinitionService workflowDefinitionService,
        IApiSerializer serializer,
        WorkflowDefinitionMapper workflowDefinitionMapper,
        VariableDefinitionMapper variableDefinitionMapper)
    {
        _store = store;
        _serializer = serializer;
        _workflowDefinitionMapper = workflowDefinitionMapper;
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
            await DownloadSingleWorkflowAsync(request.DefinitionId, request.VersionOptions, cancellationToken);
        else
            await DownloadMultipleWorkflowsAsync(request.Ids, cancellationToken);
    }

    private async Task DownloadMultipleWorkflowsAsync(ICollection<string> ids, CancellationToken cancellationToken)
    {
        List<WorkflowDefinition> definitions = (await _store.FindManyAsync(new WorkflowDefinitionFilter
        {
            Ids = ids
        }, cancellationToken)).ToList();

        if (!definitions.Any())
        {
            await SendNoContentAsync(cancellationToken);
            return;
        }

        var zipStream = new MemoryStream();
        using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            // Create a JSON file for each workflow definition:
            foreach (var definition in definitions)
            {
                var model = await CreateWorkflowModelAsync(definition, cancellationToken);
                var binaryJson = SerializeWorkflowDefinition(model);
                var fileName = GetFileName(model);
                var entry = zipArchive.CreateEntry(fileName, CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await entryStream.WriteAsync(binaryJson, cancellationToken);
            }
        }

        // Send the zip file to the client:
        zipStream.Position = 0;
        await SendBytesAsync(zipStream.ToArray(), "workflow-definitions.zip", cancellation: cancellationToken);
    }

    private async Task DownloadSingleWorkflowAsync(string definitionId, string? versionOptions, CancellationToken cancellationToken)
    {
        var parsedVersionOptions = string.IsNullOrEmpty(versionOptions) ? VersionOptions.Latest : VersionOptions.FromString(versionOptions);
        WorkflowDefinition? definition = (await _store.FindManyAsync(new WorkflowDefinitionFilter
        {
            DefinitionId = definitionId,
            VersionOptions = parsedVersionOptions
        }, cancellationToken)).FirstOrDefault();

        if (definition == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var model = await CreateWorkflowModelAsync(definition, cancellationToken);
        var binaryJson = SerializeWorkflowDefinition(model);
        var fileName = GetFileName(model);

        await SendBytesAsync(binaryJson, fileName, cancellation: cancellationToken);
    }

    private string GetFileName(WorkflowDefinitionModel definition)
    {
        var hasWorkflowName = !string.IsNullOrWhiteSpace(definition.Name);
        var workflowName = hasWorkflowName ? definition.Name!.Trim() : definition.DefinitionId;
        var fileName = $"workflow-definition-{workflowName.Underscore().Dasherize().ToLowerInvariant()}.json";
        return fileName;
    }

    private byte[] SerializeWorkflowDefinition(WorkflowDefinitionModel model)
    {
        JsonSerializerOptions serializerOptions = _serializer.GetOptions();
        var binaryJson = JsonSerializer.SerializeToUtf8Bytes(model, serializerOptions);
        return binaryJson;
    }

    private async Task<WorkflowDefinitionModel> CreateWorkflowModelAsync(WorkflowDefinition definition, CancellationToken cancellationToken)
    {
        return await _workflowDefinitionMapper.MapAsync(definition, cancellationToken);
    }
}