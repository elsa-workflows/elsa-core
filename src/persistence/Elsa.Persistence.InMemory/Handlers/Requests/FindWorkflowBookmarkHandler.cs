using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Implementations;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.InMemory.Handlers.Requests;

public class FindWorkflowBookmarkHandler : IRequestHandler<FindWorkflowBookmark, WorkflowBookmark?>
{
    private readonly InMemoryStore<WorkflowBookmark> _store;
    public FindWorkflowBookmarkHandler(InMemoryStore<WorkflowBookmark> store) => _store = store;
    public Task<WorkflowBookmark?> HandleAsync(FindWorkflowBookmark request, CancellationToken cancellationToken) => Task.FromResult(FindById(request.BookmarkId));
    private WorkflowBookmark? FindById(string id) => _store.Find(x => x.Id == id);
}