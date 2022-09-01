using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public override async Task<Unit> Store(StoreBookmarkRequest request)
    {
        var bookmark = new StoredBookmark
        {
            BookmarkId = request.BookmarkId,
            WorkflowInstanceId = request.WorkflowInstanceId
        };

        await _persistence.PersistEventAsync(new BookmarkStored(bookmark));
        return new Unit();
    }

    public override async Task<Unit> Remove(RemoveBookmarkRequest request)
    {
        await _persistence.PersistEventAsync(new BookmarkRemoved(request.BookmarkId));
        return new Unit();
    }

    public override Task<ResolveBookmarkResponse> Resolve(ResolveBookmarkRequest request)
    {
        var response = new ResolveBookmarkResponse();
        response.Bookmarks.AddRange(_bookmarks);

        return Task.FromResult(response);
    }

    private void AddBookmark(StoredBookmark bookmark) => _bookmarks.Add(bookmark);

    private void RemoveBookmark(string bookmarkId)
    {
        var bookmark = _bookmarks.FirstOrDefault(x => x.BookmarkId == bookmarkId);
        if (bookmark != null) _bookmarks.Remove(bookmark);
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
            case BookmarkStored bookmarkStored:
                AddBookmark(bookmarkStored.Bookmark);
                break;
            case BookmarkRemoved bookmarkRemoved:
                RemoveBookmark(bookmarkRemoved.BookmarkId);
                break;
        }
    }

    private object GetState() => new BookmarkSnapshot(_bookmarks);
}