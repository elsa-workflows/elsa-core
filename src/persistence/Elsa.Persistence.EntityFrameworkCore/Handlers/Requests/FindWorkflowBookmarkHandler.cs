using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Services;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Requests;

public class FindWorkflowBookmarkHandler : IRequestHandler<FindWorkflowBookmark, WorkflowBookmark?>
{
    private readonly IStore<WorkflowBookmark> _store;
    public FindWorkflowBookmarkHandler(IStore<WorkflowBookmark> store) => _store = store;
    public async Task<WorkflowBookmark?> HandleAsync(FindWorkflowBookmark request, CancellationToken cancellationToken) => await _store.FindAsync(x => x.Id == request.BookmarkId, cancellationToken);
}