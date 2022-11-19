using System.Text.Json;
using Elsa.Common.Models;
using Elsa.ProtoActor.Extensions;
using Elsa.Runtime.Protos;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Services;
using Proto.Cluster;

namespace Elsa.ProtoActor.Implementations;

public class ProtoActorWorkflowRuntime : IWorkflowRuntime
{
    private readonly Cluster _cluster;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;
    private readonly ITriggerStore _triggerStore;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly IBookmarkHasher _hasher;

    public ProtoActorWorkflowRuntime(
        Cluster cluster,
        SerializerOptionsProvider serializerOptionsProvider,
        ITriggerStore triggerStore,
        IIdentityGenerator identityGenerator,
        IBookmarkHasher hasher)
    {
        _cluster = cluster;
        _serializerOptionsProvider = serializerOptionsProvider;
        _triggerStore = triggerStore;
        _identityGenerator = identityGenerator;
        _hasher = hasher;
    }

    public async Task<StartWorkflowResult> StartWorkflowAsync(
        string definitionId,
        StartWorkflowRuntimeOptions options,
        CancellationToken cancellationToken = default)
    {
        var versionOptions = options.VersionOptions;
        var correlationId = options.CorrelationId;
        var input = options.Input;

        var request = new StartWorkflowRequest
        {
            DefinitionId = definitionId,
            VersionOptions = versionOptions.ToString(),
            CorrelationId = correlationId.WithDefault(""),
            Input = input?.Serialize(),
            TriggerActivityId = options.TriggerActivityId.WithDefault("")
        };

        var workflowInstanceId = _identityGenerator.GenerateId();
        var client = _cluster.GetWorkflowGrain(workflowInstanceId);
        var response = await client.Start(request, cancellationToken);
        var bookmarks = Map(response!.Bookmarks).ToList();

        return new StartWorkflowResult(workflowInstanceId, bookmarks);
    }

    public async Task<ResumeWorkflowResult> ResumeWorkflowAsync(
        string workflowInstanceId,
        ResumeWorkflowRuntimeOptions options,
        CancellationToken cancellationToken = default)
    {
        var request = new ResumeWorkflowRequest
        {
            InstanceId = workflowInstanceId,
            CorrelationId = options.CorrelationId,
            BookmarkId = options.BookmarkId,
            ActivityId = options.ActivityId,
            Input = options.Input?.Serialize()
        };

        var client = _cluster.GetWorkflowGrain(workflowInstanceId);
        var response = await client.Resume(request, cancellationToken);
        var bookmarks = Map(response!.Bookmarks).ToList();

        return new ResumeWorkflowResult(bookmarks);
    }

    public async Task<ICollection<ResumedWorkflow>> ResumeWorkflowsAsync(string activityTypeName, object bookmarkPayload, ResumeWorkflowRuntimeOptions options, CancellationToken cancellationToken = default)
    {
        var hash = _hasher.Hash(activityTypeName, bookmarkPayload);
        var client = _cluster.GetBookmarkGrain(hash);

        var request = new ResolveBookmarksRequest
        {
            ActivityTypeName = activityTypeName,
            CorrelationId = options.CorrelationId ?? ""
        };

        var bookmarksResponse = await client.Resolve(request, cancellationToken);
        var bookmarks = bookmarksResponse!.Bookmarks;
        return await ResumeWorkflowsAsync(bookmarks, options, cancellationToken);
    }

    public async Task<ICollection<ResumedWorkflow>> ResumeWorkflowsAsync(IEnumerable<StoredBookmark> bookmarks, ResumeWorkflowRuntimeOptions runtimeOptions, CancellationToken cancellationToken = default)
    {
        var resumedWorkflows = new List<ResumedWorkflow>();

        foreach (var bookmark in bookmarks)
        {
            var workflowInstanceId = bookmark.WorkflowInstanceId;

            var resumeResult = await ResumeWorkflowAsync(
                workflowInstanceId,
                runtimeOptions,
                cancellationToken);

            resumedWorkflows.Add(new ResumedWorkflow(workflowInstanceId, resumeResult.Bookmarks));
        }

        return resumedWorkflows;
    }

    public async Task<TriggerWorkflowsResult> TriggerWorkflowsAsync(
        string activityTypeName,
        object bookmarkPayload,
        TriggerWorkflowsRuntimeOptions options,
        CancellationToken cancellationToken = default)
    {
        var triggeredWorkflows = new List<TriggeredWorkflow>();
        var hash = _hasher.Hash(activityTypeName, bookmarkPayload);

        // Resume existing workflow instances.
        var client = _cluster.GetBookmarkGrain(hash);

        var request = new ResolveBookmarksRequest
        {
            ActivityTypeName = activityTypeName,
            CorrelationId = options.CorrelationId ?? ""
        };

        var bookmarksResponse = await client.Resolve(request, cancellationToken);
        var bookmarks = bookmarksResponse!.Bookmarks;

        foreach (var bookmark in bookmarks)
        {
            var workflowInstanceId = bookmark.WorkflowInstanceId;

            var resumeResult = await ResumeWorkflowAsync(
                workflowInstanceId,
                new ResumeWorkflowRuntimeOptions(options.CorrelationId, bookmark.BookmarkId, null, options.Input),
                cancellationToken);

            triggeredWorkflows.Add(new TriggeredWorkflow(workflowInstanceId, resumeResult.Bookmarks));
        }

        // Start new workflows.
        var triggers = await _triggerStore.FindAsync(hash, cancellationToken);

        foreach (var trigger in triggers)
        {
            var startResult = await StartWorkflowAsync(
                trigger.WorkflowDefinitionId,
                new StartWorkflowRuntimeOptions(options.CorrelationId, options.Input, VersionOptions.Published, trigger.ActivityId),
                cancellationToken);

            triggeredWorkflows.Add(new TriggeredWorkflow(startResult.InstanceId, startResult.Bookmarks));
        }

        return new TriggerWorkflowsResult(triggeredWorkflows);
    }

    public async Task<WorkflowState?> ExportWorkflowStateAsync(
        string workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        var client = _cluster.GetWorkflowGrain(workflowInstanceId);
        var response = await client.ExportState(new ExportWorkflowStateRequest(), cancellationToken);
        var json = response!.SerializedWorkflowState.Text;
        var options = _serializerOptionsProvider.CreatePersistenceOptions();
        var workflowState = JsonSerializer.Deserialize<WorkflowState>(json, options);
        return workflowState;
    }

    /// <inheritdoc />
    public async Task ImportWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var options = _serializerOptionsProvider.CreatePersistenceOptions();
        var client = _cluster.GetWorkflowGrain(workflowState.Id);
        var json = JsonSerializer.Serialize(workflowState, options);

        var request = new ImportWorkflowStateRequest
        {
            SerializedWorkflowState = new Json
            {
                Text = json
            }
        };

        await client.ImportState(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateBookmarksAsync(UpdateBookmarksContext context, CancellationToken cancellationToken = default)
    {
        await RemoveBookmarksAsync(context.InstanceId, context.Diff.Removed, cancellationToken);
        await StoreBookmarksAsync(context.InstanceId, context.Diff.Added, context.CorrelationId, cancellationToken);
    }

    private async Task StoreBookmarksAsync(string instanceId, ICollection<Bookmark> bookmarks, string? correlationId, CancellationToken cancellationToken = default)
    {
        var groupedBookmarks = bookmarks.GroupBy(x => x.Hash);

        foreach (var groupedBookmark in groupedBookmarks)
        {
            var bookmarkClient = _cluster.GetBookmarkGrain(groupedBookmark.Key);

            var storeBookmarkRequest = new StoreBookmarksRequest
            {
                WorkflowInstanceId = instanceId,
                CorrelationId = correlationId
            };

            storeBookmarkRequest.BookmarkIds.AddRange(groupedBookmark.Select(x => x.Id));
            await bookmarkClient.Store(storeBookmarkRequest, cancellationToken);
        }
    }

    private async Task RemoveBookmarksAsync(string instanceId, IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var groupedBookmarks = bookmarks.GroupBy(x => x.Hash);

        foreach (var groupedBookmark in groupedBookmarks)
        {
            var bookmarkClient = _cluster.GetBookmarkGrain(groupedBookmark.Key);
            await bookmarkClient.RemoveByWorkflow(new RemoveBookmarksByWorkflowRequest
            {
                WorkflowInstanceId = instanceId
            }, cancellationToken);
        }
    }

    private static IEnumerable<Bookmark> Map(IEnumerable<BookmarkDto> bookmarkDtos) =>
        bookmarkDtos.Select(x =>
            new Bookmark(
                x.Id,
                x.Name,
                x.Hash,
                x.Data.NullIfEmpty(),
                x.ActivityId,
                x.ActivityInstanceId,
                x.CallbackMethodName.NullIfEmpty()));
}