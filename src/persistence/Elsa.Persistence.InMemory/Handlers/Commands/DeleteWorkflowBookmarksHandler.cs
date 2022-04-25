using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Services;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Implementations;

namespace Elsa.Persistence.InMemory.Handlers.Commands;

public class DeleteWorkflowBookmarksHandler : ICommandHandler<DeleteWorkflowBookmarks, int>
{
    private readonly InMemoryStore<WorkflowBookmark> _store;

    public DeleteWorkflowBookmarksHandler(InMemoryStore<WorkflowBookmark> store)
    {
        _store = store;
    }

    public Task<int> HandleAsync(DeleteWorkflowBookmarks command, CancellationToken cancellationToken)
    {
        var count = _store.DeleteMany(command.BookmarkIds);

        return Task.FromResult(count);
    }
}