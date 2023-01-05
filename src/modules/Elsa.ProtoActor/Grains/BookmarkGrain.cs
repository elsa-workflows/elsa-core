using Elsa.Extensions;
using Elsa.ProtoActor.Extensions;
using Elsa.ProtoActor.Protos;
using Google.Protobuf.WellKnownTypes;
using Proto;
using Proto.Cluster;
using Proto.Persistence;
using Proto.Persistence.SnapshotStrategies;

namespace Elsa.ProtoActor.Grains;

/// <summary>
/// Represents a registry of bookmarks.
/// </summary>
public class BookmarkGrain : BookmarkGrainBase
{
    private const int EventsPerSnapshot = 100;
    private ICollection<StoredBookmark> _bookmarks = new List<StoredBookmark>();
    private readonly Persistence _persistence;

    /// <inheritdoc />
    public BookmarkGrain(IProvider provider, IContext context) : base(context)
    {
        _persistence = Persistence.WithEventSourcingAndSnapshotting(
            provider,
            provider,
            BookmarkHash,
            ApplyEvent,
            ApplySnapshot,
            new IntervalStrategy(EventsPerSnapshot),
            GetState);
    }

    private string BookmarkHash => Context.ClusterIdentity()!.Identity;

    /// <inheritdoc />
    public override async Task OnStarted() => await _persistence.RecoverStateAsync();

    /// <inheritdoc />
    public override async Task<Empty> Store(StoreBookmarksRequest request)
    {
        var bookmarks = request.BookmarkIds.Select(x => new StoredBookmark
        {
            WorkflowInstanceId = request.WorkflowInstanceId,
            CorrelationId = request.CorrelationId,
            BookmarkId = x
        }).ToList();

        await _persistence.PersistRollingEventAsync(new BookmarksStored(bookmarks), EventsPerSnapshot);
        return new Empty();
    }

    /// <inheritdoc />
    public override async Task<Empty> RemoveByWorkflow(RemoveBookmarksByWorkflowRequest request)
    {
        await _persistence.PersistRollingEventAsync(new BookmarksRemovedByWorkflow(request.WorkflowInstanceId), EventsPerSnapshot);
        
        return new Empty();
    }

    /// <inheritdoc />
    public override Task<ResolveBookmarksResponse> Resolve(ResolveBookmarksRequest request)
    {
        var response = new ResolveBookmarksResponse();
        var query = _bookmarks.AsQueryable();

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