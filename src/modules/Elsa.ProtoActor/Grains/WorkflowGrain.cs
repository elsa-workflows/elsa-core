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
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Services;
using Proto;
using Proto.Cluster;
using Bookmark = Elsa.Workflows.Core.Models.Bookmark;

namespace Elsa.ProtoActor.Grains;

/// <summary>
/// Executes a workflow.
/// </summary>
public class WorkflowGrain : WorkflowGrainBase
{
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IEventPublisher _eventPublisher;
    private Workflow _workflow = default!;
    private WorkflowState _workflowState = default!;
    private ICollection<Bookmark> _bookmarks = new List<Bookmark>();

    public WorkflowGrain(
        IWorkflowDefinitionService workflowDefinitionService, 
        IWorkflowRunner workflowRunner,
        IEventPublisher eventPublisher,
        IContext context) : base(context)
    {
        _workflowDefinitionService = workflowDefinitionService;
        _workflowRunner = workflowRunner;
        _eventPublisher = eventPublisher;
    }

    private string WorkflowInstanceId => Context.ClusterIdentity()!.Identity;

    public override async Task<StartWorkflowResponse> Start(StartWorkflowRequest request)
    {
        var definitionId = request.DefinitionId;
        var versionOptions = VersionOptions.FromString(request.VersionOptions);
        var correlationId = request.CorrelationId == "" ? default : request.CorrelationId;
        var input = request.Input?.Deserialize();
        var cancellationToken = Context.CancellationToken;

        var workflowDefinition = await _workflowDefinitionService.FindAsync(definitionId, versionOptions, cancellationToken);

        if (workflowDefinition == null)
            return new StartWorkflowResponse
            {
                NotFound = true
            };

        _workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        var workflowResult = await _workflowRunner.RunAsync(_workflow, WorkflowInstanceId, input, cancellationToken);
        var finished = workflowResult.WorkflowState.Status == WorkflowStatus.Finished;

        _workflowState = workflowResult.WorkflowState;
        await StoreBookmarksAsync(workflowResult.Bookmarks, cancellationToken);

        return new StartWorkflowResponse
        {
            Finished = finished
        };
    }

    public override async Task<ResumeWorkflowResponse> Resume(ResumeWorkflowRequest request)
    {
        var input = request.Input?.Deserialize();
        var cancellationToken = Context.CancellationToken;
        var bookmark = _bookmarks.FirstOrDefault(x => x.Id == request.BookmarkId);

        if (bookmark == null)
            return new ResumeWorkflowResponse
            {
                Status = ResumeWorkflowStatus.BookmarkNotFound
            };

        var workflowResult = await _workflowRunner.RunAsync(_workflow, _workflowState, bookmark, input, cancellationToken);
        var finished = workflowResult.WorkflowState.Status == WorkflowStatus.Finished;

        _workflowState = workflowResult.WorkflowState;

        await BurnBookmarkAsync(bookmark, cancellationToken);
        await StoreBookmarksAsync(workflowResult.Bookmarks, cancellationToken);

        return new ResumeWorkflowResponse
        {
            Status = finished ? ResumeWorkflowStatus.Finished : ResumeWorkflowStatus.Suspended
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
}