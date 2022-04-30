using Elsa.Mediator.Models;
using Elsa.Mediator.Services;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Services;

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