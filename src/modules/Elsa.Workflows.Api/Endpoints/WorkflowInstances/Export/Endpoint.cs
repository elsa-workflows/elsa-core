using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Abstractions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Endpoints.WorkflowInstances.Get;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Elsa.Workflows.State;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Export;

/// <summary>
/// Exports the specified workflow instances as JSON downloads. When selecting multiple instances, a zip file will be downloaded.
/// </summary>
[UsedImplicitly]
internal class Export : ElsaEndpointWithMapper<Request, WorkflowInstanceMapper>
{
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly IActivityExecutionStore _activityExecutionStore;
    private readonly IWorkflowExecutionLogStore _workflowExecutionLogStore;
    private readonly IBookmarkStore _bookmarkStore;
    private readonly IWorkflowStateSerializer _workflowStateSerializer;
    private readonly IPayloadSerializer _payloadSerializer;
    private readonly ISafeSerializer _safeSerializer;

    /// <inheritdoc />
    public Export(
        IWorkflowInstanceStore workflowInstanceStore,
        IActivityExecutionStore activityExecutionStore,
        IWorkflowExecutionLogStore workflowExecutionLogStore,
        IBookmarkStore bookmarkStore,
        IWorkflowStateSerializer workflowStateSerializer,
        IPayloadSerializer payloadSerializer,
        ISafeSerializer safeSerializer)
    {
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
        Routes("/bulk-actions/export/workflow-instances", "/workflow-instances/{id}/export");
        Verbs(FastEndpoints.Http.GET, FastEndpoints.Http.POST);
        ConfigurePermissions("read:workflow-instances");
    }

    /// <inheritdoc />
    public override Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        if (request.Id != null || request.Ids.Count == 1)
            return DownloadSingleInstanceAsync(request, request.Id ?? request.Ids.First(), cancellationToken);
        return DownloadMultipleInstancesAsync(request, cancellationToken);
    }

    private async Task DownloadMultipleInstancesAsync(Request request, CancellationToken cancellationToken)
    {
        var instances = (await _workflowInstanceStore.FindManyAsync(new WorkflowInstanceFilter { Ids = request.Ids }, cancellationToken: cancellationToken)).ToList();

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
                var model = await CreateExportModelAsync(request, instance, cancellationToken);
                var binaryJson = SerializeWorkflowInstance(model);
                var fileName = GetFileName(instance.WorkflowState);
                var entry = zipArchive.CreateEntry(fileName, CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await entryStream.WriteAsync(binaryJson, cancellationToken);
            }
        }

        // Send the zip file to the client:
        zipStream.Position = 0;
        await SendBytesAsync(zipStream.ToArray(), "workflow-instances.zip", cancellation: cancellationToken);
    }

    private async Task DownloadSingleInstanceAsync(Request request, string id, CancellationToken cancellationToken)
    {
        var instance = (await _workflowInstanceStore.FindManyAsync(new WorkflowInstanceFilter { Id = id }, cancellationToken: cancellationToken)).FirstOrDefault();

        if (instance == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var model = await CreateExportModelAsync(request, instance, cancellationToken);
        var binaryJson = SerializeWorkflowInstance(model);
        var fileName = GetFileName(instance.WorkflowState);

        await SendBytesAsync(binaryJson, fileName, cancellation: cancellationToken);
    }

    private async Task<ExportedWorkflowState> CreateExportModelAsync(Request request, WorkflowInstance instance, CancellationToken cancellationToken)
    {
        var workflowState = instance.WorkflowState;
        var executionLogRecords = request.IncludeWorkflowExecutionLog ? await LoadWorkflowExecutionLogRecordsAsync(workflowState.Id, cancellationToken) : default;
        var activityExecutionLogRecords = request.IncludeActivityExecutionLog ? await LoadActivityExecutionLogRecordsAsync(workflowState.Id, cancellationToken) : default;
        var bookmarks = request.IncludeBookmarks ? await LoadBookmarksAsync(workflowState.Id, cancellationToken) : null;
        var workflowStateElement = _workflowStateSerializer.SerializeToElement(workflowState);
        var bookmarksElement = bookmarks != null ? SerializeBookmarks(bookmarks) : default(JsonElement?);
        var executionLogRecordsElement = executionLogRecords != null ? await _safeSerializer.SerializeToElementAsync(executionLogRecords, cancellationToken) : default(JsonElement?);
        var activityExecutionLogRecordsElement = activityExecutionLogRecords != null ? await _safeSerializer.SerializeToElementAsync(activityExecutionLogRecords, cancellationToken) : default(JsonElement?);
        var model = new ExportedWorkflowState(workflowStateElement, bookmarksElement, activityExecutionLogRecordsElement, executionLogRecordsElement);
        return model;
    }

    private JsonElement SerializeBookmarks(IEnumerable<StoredBookmark> bookmarks)
    {
        var jsonBookmarkNodes = bookmarks.Select(x => new JsonObject
        {
            ["id"] = x.BookmarkId,
            ["activityTypeName"] = x.ActivityTypeName,
            ["workflowInstanceId"] = x.WorkflowInstanceId,
            ["activityInstanceId"] = x.ActivityInstanceId,
            ["hash"] = x.Hash,
            ["correlationId"] = x.CorrelationId,
            ["createdAt"] = x.CreatedAt,
            ["payload"] = JsonObject.Create(_payloadSerializer.SerializeToElement(x.Payload!)),
            ["metadata"] = JsonObject.Create(_payloadSerializer.SerializeToElement(x.Metadata!))
        }).Cast<JsonNode>().ToArray();

        var jsonBookmarkArray = new JsonArray(jsonBookmarkNodes);
        return JsonSerializer.SerializeToElement(jsonBookmarkArray);
    }

    private async Task<IEnumerable<StoredBookmark>> LoadBookmarksAsync(string workflowInstanceId, CancellationToken cancellationToken)
    {
        var filter = new BookmarkFilter { WorkflowInstanceId = workflowInstanceId };
        return await _bookmarkStore.FindManyAsync(filter, cancellationToken);
    }

    private async Task<IEnumerable<ActivityExecutionRecord>> LoadActivityExecutionLogRecordsAsync(string workflowInstanceId, CancellationToken cancellationToken)
    {
        var filter = new ActivityExecutionRecordFilter { WorkflowInstanceId = workflowInstanceId };
        var order = new ActivityExecutionRecordOrder<DateTimeOffset>(x => x.StartedAt, OrderDirection.Ascending);
        return await _activityExecutionStore.FindManyAsync(filter, order, cancellationToken);
    }

    private async Task<IEnumerable<WorkflowExecutionLogRecord>> LoadWorkflowExecutionLogRecordsAsync(string workflowInstanceId, CancellationToken cancellationToken)
    {
        var filter = new WorkflowExecutionLogRecordFilter { WorkflowInstanceId = workflowInstanceId };
        var order = new WorkflowExecutionLogRecordOrder<DateTimeOffset>(x => x.Timestamp, OrderDirection.Ascending);
        var page = await _workflowExecutionLogStore.FindManyAsync(filter, PageArgs.All, order, cancellationToken);
        return page.Items;
    }

    private static string GetFileName(WorkflowState instance)
    {
        var fileName = $"workflow-instance-{instance.Id.ToLowerInvariant()}.json";
        return fileName;
    }

    private static byte[] SerializeWorkflowInstance(ExportedWorkflowState model)
    {
        var binaryJson = JsonSerializer.SerializeToUtf8Bytes(model);    
        return binaryJson;
    }
}