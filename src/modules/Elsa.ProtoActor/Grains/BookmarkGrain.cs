using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Runtime.Protos;
using Proto;

namespace Elsa.ProtoActor.Grains;

public class BookmarkGrain : BookmarkGrainBase
{
    private readonly ICollection<StoredBookmark> _bookmarks = new List<StoredBookmark>();

    public BookmarkGrain(IContext context) : base(context)
    {
    }

    public override Task<Unit> Store(StoreBookmarkRequest request)
    {
        _bookmarks.Add(new StoredBookmark
        {
            BookmarkId = request.BookmarkId,
            WorkflowInstanceId = request.WorkflowInstanceId
        });

        return Task.FromResult(new Unit());
    }

    public override Task<Unit> Remove(RemoveBookmarkRequest request)
    {
        var bookmark = _bookmarks.FirstOrDefault(x => x.BookmarkId == request.BookmarkId);
        if (bookmark != null) _bookmarks.Remove(bookmark);

        return Task.FromResult(new Unit());
    }

    public override Task<ResolveBookmarkResponse> Resolve(ResolveBookmarkRequest request)
    {
        var response = new ResolveBookmarkResponse();
        response.Bookmarks.AddRange(_bookmarks);

        return Task.FromResult(response);
    }
}