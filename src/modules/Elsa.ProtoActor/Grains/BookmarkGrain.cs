using Elsa.Common.Extensions;
using Elsa.Runtime.Protos;
using Proto;
using Proto.Cluster;
using Proto.Persistence;
using Proto.Persistence.SnapshotStrategies;

namespace Elsa.ProtoActor.Grains;

using Persistence = Proto.Persistence.Persistence;

public class BookmarkGrain : BookmarkGrainBase
{
    private ICollection<StoredBookmark> _bookmarks = new List<StoredBookmark>();
    private readonly Persistence _persistence;

    public BookmarkGrain(IProvider provider, IContext context) : base(context)
    {
        _persistence = Persistence.WithEventSourcingAndSnapshotting(
            provider,
            provider,
            BookmarkHash,
            ApplyEvent,
            ApplySnapshot,
            new TimeStrategy(TimeSpan.FromSeconds(10)),
            GetState);
    }

    private string BookmarkHash => Context.ClusterIdentity()!.Identity;
    public override async Task OnStarted() => await _persistence.RecoverStateAsync();

    public override async Task<Unit> Store(StoreBookmarksRequest request)
    {
        var bookmarks = request.BookmarkIds.Select(x => new StoredBookmark
        {
            WorkflowInstanceId = request.WorkflowInstanceId,
            CorrelationId = request.CorrelationId,
            BookmarkId = x
        }).ToList();

        await _persistence.PersistEventAsync(new BookmarksStored(bookmarks));
        return new Unit();
    }

    public override async Task<Unit> RemoveByWorkflow(RemoveBookmarksByWorkflowRequest request)
    {
        await _persistence.PersistEventAsync(new BookmarksRemovedByWorkflow(request.WorkflowInstanceId));
        return new Unit();
    }

    public override Task<ResolveBookmarksResponse> Resolve(ResolveBookmarksRequest request)
    {
        var response = new ResolveBookmarksResponse();
        var query = response.Bookmarks.AsQueryable();

        if (!string.IsNullOrEmpty(request.CorrelationId))
            query = query.Where(x => x.CorrelationId == request.CorrelationId);
        
        response.Bookmarks.AddRange(query);

        return Task.FromResult(response);
    }
    
    private void ApplySnapshot(Snapshot snapshot)
    {
        var bookmarkSnapshot = (BookmarkSnapshot)snapshot.State;
        _bookmarks = bookmarkSnapshot.Bookmarks;
    }

    private void ApplyEvent(Event @event)
    {
        switch (@event.Data)
        {
            case BookmarksStored bookmarksStored:
                _bookmarks.AddRange(bookmarksStored.Bookmarks);
                break;
            case BookmarksRemovedByWorkflow bookmarksRemovedByWorkflow:
                _bookmarks.RemoveWhere(x => x.WorkflowInstanceId == bookmarksRemovedByWorkflow.WorkflowInstanceId);
                break;
        }
    }

    private object GetState() => new BookmarkSnapshot(_bookmarks);
}