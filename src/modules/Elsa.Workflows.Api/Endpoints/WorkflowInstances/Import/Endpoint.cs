using System.IO.Compression;
using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Import;

/// <summary>
/// Imports JSON and/or ZIP files containing a workflow instances.
/// </summary>
[PublicAPI]
internal class Import : ElsaEndpointWithoutRequest<Response>
{
    private readonly IWorkflowInstanceManager _workflowInstanceManager;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly IActivityExecutionStore _activityExecutionStore;
    private readonly IWorkflowExecutionLogStore _workflowExecutionLogStore;
    private readonly IBookmarkStore _bookmarkStore;
    private readonly IWorkflowStateSerializer _workflowStateSerializer;
    private readonly IPayloadSerializer _payloadSerializer;
    private readonly ISafeSerializer _safeSerializer;

    /// <inheritdoc />
    public Import(
        IWorkflowInstanceManager workflowInstanceManager,
        IWorkflowInstanceStore workflowInstanceStore,
        IActivityExecutionStore activityExecutionStore,
        IWorkflowExecutionLogStore workflowExecutionLogStore,
        IBookmarkStore bookmarkStore,
        IWorkflowStateSerializer workflowStateSerializer,
        IPayloadSerializer payloadSerializer,
        ISafeSerializer safeSerializer)
    {
        _workflowInstanceManager = workflowInstanceManager;
        _workflowInstanceStore = workflowInstanceStore;
        _activityExecutionStore = activityExecutionStore;
        _workflowExecutionLogStore = workflowExecutionLogStore;
        _bookmarkStore = bookmarkStore;
        _workflowStateSerializer = workflowStateSerializer;
        _payloadSerializer = payloadSerializer;
        _safeSerializer = safeSerializer;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/bulk-actions/import/workflow-instances");
        ConfigurePermissions("write:workflow-instances");
        AllowFileUploads();
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var count = await ImportFilesAsync(Files, cancellationToken);
        var response = new Response { Imported = count };

        await SendOkAsync(response, cancellationToken);
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
        var model = JsonSerializer.Deserialize<ExportedWorkflowState>(json)!;
        await ImportSingleWorkflowInstanceAsync(model, cancellationToken);
    }

    private async Task ImportSingleWorkflowInstanceAsync(ExportedWorkflowState model, CancellationToken cancellationToken)
    {
        var workflowState = _workflowStateSerializer.Deserialize(model.WorkflowState);
        await _workflowInstanceManager.SaveAsync(workflowState, cancellationToken);

        if (model.Bookmarks != null)
        {
            var bookmarksElement = model.Bookmarks.Value.EnumerateArray().ToList();
            var bookmarks = bookmarksElement.Select(x =>
            {
                var bookmarkId = x.GetProperty("id").GetString()!;
                var activityTypeName = x.GetProperty("activityTypeName").GetString()!;
                var workflowInstanceId = x.GetProperty("workflowInstanceId").GetString()!;
                var activityInstanceId = x.GetProperty("activityInstanceId").GetString();
                var hash = x.GetProperty("hash").GetString()!;
                var correlationId = x.GetProperty("correlationId").GetString();
                var createdAt = x.GetProperty("createdAt").GetDateTimeOffset();
                var payloadElement = x.GetProperty("payload");
                var metadataElement = x.GetProperty("metadata");
                var payload = _payloadSerializer.Deserialize<object>(payloadElement);
                var metadata = _payloadSerializer.Deserialize<IDictionary<string, string>>(metadataElement);

                return new StoredBookmark
                {
                    Id = bookmarkId,
                    Name = activityTypeName,
                    Hash = hash,
                    WorkflowInstanceId = workflowInstanceId,
                    CreatedAt = createdAt,
                    ActivityInstanceId = activityInstanceId,
                    CorrelationId = correlationId,
                    Payload = payload,
                    Metadata = metadata
                };
            }).ToList();
            await _bookmarkStore.SaveManyAsync(bookmarks, cancellationToken);
        }
        
        if (model.ActivityExecutionRecords != null)
        {
            var activityExecutionRecords = _safeSerializer.Deserialize<ICollection<ActivityExecutionRecord>>(model.ActivityExecutionRecords.Value);
            await _activityExecutionStore.SaveManyAsync(activityExecutionRecords, cancellationToken);
        }
        
        if (model.WorkflowExecutionLogRecords != null)
        {
            var workflowExecutionLogRecords = _safeSerializer.Deserialize<ICollection<WorkflowExecutionLogRecord>>(model.WorkflowExecutionLogRecords.Value);
            await _workflowExecutionLogStore.SaveManyAsync(workflowExecutionLogRecords, cancellationToken);
        }
    }
}