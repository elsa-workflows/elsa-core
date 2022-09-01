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
using Proto.Persistence.SnapshotStrategies;
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
    private string? _correlationId;
    private IDictionary<string, object>? _input;
    private string? _bookmarkId;
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
        _persistence = Persistence.WithEventSourcingAndSnapshotting(provider, provider, WorkflowInstanceId, ApplyEvent, ApplySnapshot, new TimeStrategy(TimeSpan.FromSeconds(10)), GetState);
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

        // Store event.
        await _persistence.PersistEventAsync(new WorkflowStarted(definitionId, version, correlationId, input));

        // Run the workflow.
        var workflowResult = await _workflowRunner.RunAsync(_workflow, WorkflowInstanceId, _input, cancellationToken);
        var finished = workflowResult.WorkflowState.Status == WorkflowStatus.Finished;

        _workflowState = workflowResult.WorkflowState;
        _bookmarks = workflowResult.Bookmarks;

        // Save a snapshot.
        await _persistence.PersistSnapshotAsync(GetState());

        // Store bookmarks, if any.
        await StoreBookmarksAsync(workflowResult.Bookmarks, cancellationToken);

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
        var bookmark = _bookmarks.FirstOrDefault(x => x.Id == bookmarkId);

        if (bookmark == null)
            throw new Exception("Bookmark not found");

        // Store event.
        await _persistence.PersistEventAsync(new WorkflowResumed(bookmarkId, input));
        
        // Run the workflow.
        var workflowResult = await _workflowRunner.RunAsync(_workflow, _workflowState, bookmark, input, cancellationToken);
        var finished = workflowResult.WorkflowState.Status == WorkflowStatus.Finished;

        _workflowState = workflowResult.WorkflowState;

        // Delete the resumed bookmark.
        await BurnBookmarkAsync(bookmark, cancellationToken);
        
        // Store new bookmarks, if any.
        await StoreBookmarksAsync(workflowResult.Bookmarks, cancellationToken);

        return new ResumeWorkflowResponse
        {
            Result = finished ? RunWorkflowResult.Finished : RunWorkflowResult.Suspended
        };
    }

    private async Task BurnBookmarkAsync(Bookmark bookmark, CancellationToken cancellationToken)
    {
        var hash = bookmark.Hash;
        var bookmarkClient = Context.GetBookmarkGrain(hash);

        await bookmarkClient.Remove(new RemoveBookmarkRequest
        {
            BookmarkId = bookmark.Id
        }, cancellationToken);
    }

    private async Task StoreBookmarksAsync(ICollection<Bookmark> bookmarks, CancellationToken cancellationToken)
    {
        var originalBookmarks = _bookmarks;
        _bookmarks = bookmarks;

        foreach (var bookmark in _bookmarks)
        {
            var bookmarkGrainId = bookmark.Hash;
            var bookmarkClient = Context.GetBookmarkGrain(bookmarkGrainId);

            await bookmarkClient.Store(new StoreBookmarkRequest
            {
                BookmarkId = bookmark.Id,
                WorkflowInstanceId = WorkflowInstanceId
            }, cancellationToken);
        }

        await PublishChangedBookmarksAsync(originalBookmarks, bookmarks, cancellationToken);
    }

    private async Task PublishChangedBookmarksAsync(ICollection<Bookmark> originalBookmarks, ICollection<Bookmark> updatedBookmarks, CancellationToken cancellationToken)
    {
        var diff = Diff.For(originalBookmarks, updatedBookmarks);
        var removedBookmarks = diff.Removed;
        var createdBookmarks = diff.Added;
        await _eventPublisher.PublishAsync(new WorkflowBookmarksIndexed(new IndexedWorkflowBookmarks(_workflowState, createdBookmarks, removedBookmarks)), cancellationToken);
    }

    private object GetState() => new WorkflowSnapshot(_definitionId, _version, _workflowState, _bookmarks, _input);
    private void ApplySnapshot(Snapshot snapshot) => (_definitionId, _version, _workflowState, _bookmarks, _input) = (WorkflowSnapshot)snapshot.State;

    private void ApplyEvent(Event @event)
    {
        switch (@event.Data)
        {
            case WorkflowStarted workflowStarted:
                _definitionId = workflowStarted.DefinitionId;
                _version = workflowStarted.Version;
                _correlationId = workflowStarted.CorrelationId;
                _input = workflowStarted.Input;
                break;
            case WorkflowResumed workflowResumed:
                _input = workflowResumed.Input;
                break;
        }
    }
}