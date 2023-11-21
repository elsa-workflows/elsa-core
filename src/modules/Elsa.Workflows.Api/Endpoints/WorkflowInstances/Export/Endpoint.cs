using System.IO.Compression;
using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Workflows.Api.Endpoints.WorkflowInstances.Get;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Humanizer;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Export;

/// <summary>
/// Exports the specified workflow instances as JSON downloads. When selecting multiple instances, a zip file will be downloaded.
/// </summary>
[UsedImplicitly]
internal class Export : ElsaEndpointWithMapper<Request, WorkflowInstanceMapper>
{
    private readonly IWorkflowInstanceStore _store;
    private readonly IApiSerializer _serializer;

    /// <inheritdoc />
    public Export(IWorkflowInstanceStore store, IWorkflowInstanceManager workflowDefinitionService, IApiSerializer serializer)
    {
        _store = store;
        _serializer = serializer;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Routes("/bulk-actions/export/workflow-instances", "/workflow-instances/{id}/export");
        Verbs(FastEndpoints.Http.GET, FastEndpoints.Http.POST);
        ConfigurePermissions("read:workflow-instances");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        if (request.Id != null || request.Ids.Count == 1)
            await DownloadSingleInstanceAsync(request.Id ?? request.Ids.First(), cancellationToken);
        else
            await DownloadMultipleInstancesAsync(request.Ids, cancellationToken);
    }

    private async Task DownloadMultipleInstancesAsync(ICollection<string> ids, CancellationToken cancellationToken)
    {
        var instances = (await _store.FindManyAsync(new WorkflowInstanceFilter { Ids = ids }, cancellationToken: cancellationToken)).ToList();

        if (!instances.Any())
        {
            await SendNoContentAsync(cancellationToken);
            return;
        }

        var zipStream = new MemoryStream();
        using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            // Create a JSON file for each workflow definition:
            foreach (var instance in instances)
            {
                var model = CreateWorkflowInstanceModel(instance);
                var binaryJson = SerializeWorkflowInstance(model);
                var fileName = GetFileName(model);
                var entry = zipArchive.CreateEntry(fileName, CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await entryStream.WriteAsync(binaryJson, cancellationToken);
            }
        }

        // Send the zip file to the client:
        zipStream.Position = 0;
        await SendBytesAsync(zipStream.ToArray(), "workflow-instances.zip", cancellation: cancellationToken);
    }

    private async Task DownloadSingleInstanceAsync(string id, CancellationToken cancellationToken)
    {
        var instance = (await _store.FindManyAsync(new WorkflowInstanceFilter { Id = id }, cancellationToken: cancellationToken)).FirstOrDefault();

        if (instance == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var model = CreateWorkflowInstanceModel(instance);
        var binaryJson = SerializeWorkflowInstance(model);
        var fileName = GetFileName(model);

        await SendBytesAsync(binaryJson, fileName, cancellation: cancellationToken);
    }

    private string GetFileName(WorkflowInstanceModel instance)
    {
        var hasWorkflowName = !string.IsNullOrWhiteSpace(instance.Name);
        var workflowName = hasWorkflowName ? instance.Name!.Trim() : instance.Id;
        var fileName = $"workflow-instance-{workflowName.Underscore().Dasherize().ToLowerInvariant()}.json";
        return fileName;
    }

    private byte[] SerializeWorkflowInstance(WorkflowInstanceModel model)
    {
        var serializerOptions = _serializer.CreateOptions();
        var binaryJson = JsonSerializer.SerializeToUtf8Bytes(model, serializerOptions);
        return binaryJson;
    }

    private WorkflowInstanceModel CreateWorkflowInstanceModel(WorkflowInstance instance)
    {
        return Map.FromEntity(instance);
    }
}