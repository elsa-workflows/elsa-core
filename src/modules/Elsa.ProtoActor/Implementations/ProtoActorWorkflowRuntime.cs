using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
        StartWorkflowOptions options,
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
            Input = input?.Serialize()
        };

        var workflowInstanceId = _identityGenerator.GenerateId();
        var client = _cluster.GetWorkflowGrain(workflowInstanceId);
        var response = await client.Start(request, cancellationToken);
        var bookmarks = Map(response!.Bookmarks).ToList();

        return new StartWorkflowResult(workflowInstanceId, bookmarks);
    }

    public async Task<ResumeWorkflowResult> ResumeWorkflowAsync(
        string instanceId,
        string bookmarkId,
        ResumeWorkflowOptions options,
        CancellationToken cancellationToken = default)
    {
        var request = new ResumeWorkflowRequest
        {
            InstanceId = instanceId,
            BookmarkId = bookmarkId,
            Input = options.Input?.Serialize()
        };

        var client = _cluster.GetWorkflowGrain(instanceId);
        var response = await client.Resume(request, cancellationToken);
        var bookmarks = Map(response!.Bookmarks).ToList();

        return new ResumeWorkflowResult(bookmarks);
    }

    public async Task<TriggerWorkflowsResult> TriggerWorkflowsAsync(
        string activityTypeName,
        object bookmarkPayload,
        TriggerWorkflowsOptions options,
        CancellationToken cancellationToken = default)
    {
        var triggeredWorkflows = new List<TriggeredWorkflow>();
        var hash = _hasher.Hash(activityTypeName, bookmarkPayload);

        // Start new workflows.
        var triggers = await _triggerStore.FindAsync(hash, cancellationToken);

        foreach (var trigger in triggers)
        {
            var startResult = await StartWorkflowAsync(
                trigger.WorkflowDefinitionId,
                new StartWorkflowOptions(options.CorrelationId, options.Input, VersionOptions.Published),
                cancellationToken);

            triggeredWorkflows.Add(new TriggeredWorkflow(startResult.InstanceId, startResult.Bookmarks));
        }

        // Resume existing workflow instances.
        var client = _cluster.GetBookmarkGrain(hash);
        var request = new ResolveBookmarksRequest() { BookmarkName = activityTypeName };
        var bookmarksResponse = await client.Resolve(request, cancellationToken);
        var bookmarks = bookmarksResponse!.Bookmarks;

        foreach (var bookmark in bookmarks)
        {
            var workflowInstanceId = bookmark.WorkflowInstanceId;

            var resumeResult = await ResumeWorkflowAsync(
                workflowInstanceId,
                bookmark.BookmarkId,
                new ResumeWorkflowOptions(options.Input),
                cancellationToken);

            triggeredWorkflows.Add(new TriggeredWorkflow(workflowInstanceId, resumeResult.Bookmarks));
        }

        return new TriggerWorkflowsResult(triggeredWorkflows);
    }

    public async Task<WorkflowState?> ExportWorkflowStateAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        var client = _cluster.GetWorkflowGrain(instanceId);
        var response = await client.ExportState(new ExportWorkflowStateRequest(), cancellationToken);
        var json = response!.SerializedWorkflowState.Text;
        var options = _serializerOptionsProvider.CreatePersistenceOptions();
        var workflowState = JsonSerializer.Deserialize<WorkflowState>(json, options);
        return workflowState;
    }

    public async Task ImportWorkflowStateAsync(WorkflowState workflowState,
        CancellationToken cancellationToken = default)
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