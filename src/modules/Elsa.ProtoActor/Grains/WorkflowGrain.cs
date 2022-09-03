using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Services;
using Elsa.Models;
using Elsa.ProtoActor.Extensions;
using Elsa.Runtime.Protos;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Services;
using Proto;
using Proto.Cluster;
using Proto.Persistence;
using RunWorkflowResult = Elsa.Runtime.Protos.RunWorkflowResult;

namespace Elsa.ProtoActor.Grains;

using Persistence = Proto.Persistence.Persistence;

/// <summary>
/// Executes a workflow.
/// </summary>
public class WorkflowGrain : WorkflowGrainBase
{
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IEventPublisher _eventPublisher;
    private readonly Persistence _persistence;

    private string _definitionId = default!;
    private int _version;
    private IDictionary<string, object>? _input;
    private Workflow _workflow = default!;
    private WorkflowState _workflowState = default!;
    private ICollection<Bookmark> _bookmarks = new List<Bookmark>();

    public WorkflowGrain(
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowRunner workflowRunner,
        IEventPublisher eventPublisher,
        IProvider provider,
        IContext context) : base(context)
    {
        _workflowDefinitionService = workflowDefinitionService;
        _workflowRunner = workflowRunner;
        _eventPublisher = eventPublisher;
        _persistence = Persistence.WithSnapshotting(provider, WorkflowInstanceId, ApplySnapshot);
    }

    private string WorkflowInstanceId => Context.ClusterIdentity()!.Identity;

    public override async Task OnStarted()
    {
        await _persistence.RecoverStateAsync();

        if (string.IsNullOrWhiteSpace(_definitionId))
            return; // No state yet to recover from.

        var cancellationToken = Context.CancellationToken;

        // Load the workflow definition.
        var workflowDefinition = await _workflowDefinitionService.FindAsync(_definitionId, VersionOptions.SpecificVersion(_version), cancellationToken);

        if (workflowDefinition == null)
            throw new Exception("Workflow definition is no longer available");

        // Materialize the workflow.
        _workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
    }

    public override async Task<StartWorkflowResponse> Start(StartWorkflowRequest request)
    {
        var definitionId = request.DefinitionId;
        var correlationId = request.CorrelationId == "" ? default : request.CorrelationId;
        var input = request.Input?.Deserialize();
        var versionOptions = VersionOptions.FromString(request.VersionOptions);
        var cancellationToken = Context.CancellationToken;
        var workflowDefinition = await _workflowDefinitionService.FindAsync(definitionId, versionOptions, cancellationToken);

        if (workflowDefinition == null)
            throw new Exception("Specified workflow definition and version does not exist");

        _workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        var version = workflowDefinition.Version;
        var workflowResult = await _workflowRunner.RunAsync(_workflow, WorkflowInstanceId, _input, cancellationToken);
        var finished = workflowResult.WorkflowState.Status == WorkflowStatus.Finished;

        _workflowState = workflowResult.WorkflowState;
        _version = version;
        _definitionId = definitionId;
        _input = input;

        await UpdateBookmarksAsync(_workflowState.Bookmarks, cancellationToken);
        await SaveSnapshotAsync();

        return new StartWorkflowResponse
        {
            Result = finished ? RunWorkflowResult.Finished : RunWorkflowResult.Suspended
        };
    }

    public override async Task<ResumeWorkflowResponse> Resume(ResumeWorkflowRequest request)
    {
        var input = request.Input?.Deserialize();
        var bookmarkId = request.BookmarkId;
        var cancellationToken = Context.CancellationToken;
        var workflowResult = await _workflowRunner.RunAsync(_workflow, _workflowState, bookmarkId, input, cancellationToken);
        var finished = workflowResult.WorkflowState.Status == WorkflowStatus.Finished;

        _workflowState = workflowResult.WorkflowState;
        _input = input;

        await UpdateBookmarksAsync(_workflowState.Bookmarks, cancellationToken);
        await SaveSnapshotAsync();

        return new ResumeWorkflowResponse
        {
            Result = finished ? RunWorkflowResult.Finished : RunWorkflowResult.Suspended
        };
    }

    private async Task UpdateBookmarksAsync(ICollection<Bookmark> bookmarks, CancellationToken cancellationToken)
    {
        var originalBookmarks = _bookmarks;
        _bookmarks = bookmarks;

        await RemoveBookmarksAsync(originalBookmarks, cancellationToken);
        await StoreBookmarksAsync(bookmarks, cancellationToken);
        await PublishChangedBookmarksAsync(originalBookmarks, bookmarks, cancellationToken);
    }
    
    private async Task StoreBookmarksAsync(ICollection<Bookmark> bookmarks, CancellationToken cancellationToken)
    {
        var groupedBookmarks = bookmarks.GroupBy(x => x.Hash);

        foreach (var groupedBookmark in groupedBookmarks)
        {
            var bookmarkClient = Context.GetBookmarkGrain(groupedBookmark.Key);

            var storeBookmarkRequest = new StoreBookmarksRequest
            {
                WorkflowInstanceId = WorkflowInstanceId
            };

            storeBookmarkRequest.BookmarkIds.AddRange(groupedBookmark.Select(x => x.Id));
            await bookmarkClient.Store(storeBookmarkRequest, cancellationToken);
        }
    }

    private async Task RemoveBookmarksAsync(IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken)
    {
        var groupedBookmarks = bookmarks.GroupBy(x => x.Hash);

        foreach (var groupedBookmark in groupedBookmarks)
        {
            var bookmarkClient = Context.GetBookmarkGrain(groupedBookmark.Key);
            await bookmarkClient.RemoveByWorkflow(new RemoveBookmarksByWorkflowRequest
            {
                WorkflowInstanceId = WorkflowInstanceId
            }, cancellationToken);
        }
    }

    private async Task PublishChangedBookmarksAsync(ICollection<Bookmark> originalBookmarks, ICollection<Bookmark> updatedBookmarks, CancellationToken cancellationToken)
    {
        var diff = Diff.For(originalBookmarks, updatedBookmarks);
        var removedBookmarks = diff.Removed;
        var createdBookmarks = diff.Added;
        await _eventPublisher.PublishAsync(new WorkflowBookmarksIndexed(new IndexedWorkflowBookmarks(_workflowState, createdBookmarks, removedBookmarks)), cancellationToken);
    }

    private void ApplySnapshot(Snapshot snapshot) => (_definitionId, _version, _workflowState, _bookmarks, _input) = (WorkflowSnapshot)snapshot.State;
    private async Task SaveSnapshotAsync() => await _persistence.PersistSnapshotAsync(GetState());
    private object GetState() => new WorkflowSnapshot(_definitionId, _version, _workflowState, _bookmarks, _input);
}