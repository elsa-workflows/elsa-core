using Elsa.Mediator.Contracts;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Contracts;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Commands;

public class DeleteWorkflowBookmarksHandler : ICommandHandler<DeleteWorkflowBookmarks, int>
{
    private readonly IStore<WorkflowBookmark> _store;
    public DeleteWorkflowBookmarksHandler(IStore<WorkflowBookmark> store) => _store = store;
    public async Task<int> HandleAsync(DeleteWorkflowBookmarks command, CancellationToken cancellationToken) => await _store.DeleteWhereAsync(x => command.BookmarkIds.Contains(x.Id), cancellationToken);
}