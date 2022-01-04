using Elsa.Mediator.Contracts;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Contracts;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Commands;

public class SaveWorkflowBookmarksHandler : ICommandHandler<SaveWorkflowBookmarks>
{
    private readonly IStore<WorkflowBookmark> _store;
    public SaveWorkflowBookmarksHandler(IStore<WorkflowBookmark> store) => _store = store;

    public async Task<Unit> HandleAsync(SaveWorkflowBookmarks command, CancellationToken cancellationToken)
    {
        await _store.SaveManyAsync(command.WorkflowBookmarks, cancellationToken);

        return Unit.Instance;
    }
}