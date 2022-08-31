using System.Threading.Tasks;
using Elsa.Runtime.Protos;
using Proto;

namespace Elsa.ProtoActor.Grains;

public class BookmarkGrain : BookmarkGrainBase
{
    private string _bookmarkId = default!;
    private string _workflowInstanceId = default!;

    public BookmarkGrain(IContext context) : base(context)
    {
    }

    public override Task<Unit> Store(StoreBookmarkRequest request)
    {
        _bookmarkId = request.BookmarkId;
        _workflowInstanceId = request.WorkflowInstanceId;

        return Task.FromResult(new Unit());
    }

    public override Task<ResolveBookmarkResponse> Resolve(ResolveBookmarkRequest request)
    {
        var response = new ResolveBookmarkResponse
        {
            BookmarkId = _bookmarkId,
            WorkflowInstanceId = _workflowInstanceId
        };

        return Task.FromResult(response);
    }
}