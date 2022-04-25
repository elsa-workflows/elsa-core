using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Models;
using Elsa.Mediator.Services;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Implementations;

namespace Elsa.Persistence.InMemory.Handlers.Commands;

public class SaveWorkflowBookmarksHandler : ICommandHandler<SaveWorkflowBookmarks>
{
    private readonly InMemoryStore<WorkflowBookmark> _store;

    public SaveWorkflowBookmarksHandler(InMemoryStore<WorkflowBookmark> store)
    {
        _store = store;
    }

    public Task<Unit> HandleAsync(SaveWorkflowBookmarks command, CancellationToken cancellationToken)
    {
        _store.SaveMany(command.WorkflowBookmarks);

        return Task.FromResult(Unit.Instance);
    }
}