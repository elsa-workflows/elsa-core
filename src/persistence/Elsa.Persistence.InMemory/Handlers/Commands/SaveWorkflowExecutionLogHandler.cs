using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Services;

namespace Elsa.Persistence.InMemory.Handlers.Commands;

public class SaveWorkflowExecutionLogHandler : ICommandHandler<SaveWorkflowExecutionLog>
{
    private readonly InMemoryStore<WorkflowExecutionLogRecord> _store;

    public SaveWorkflowExecutionLogHandler(InMemoryStore<WorkflowExecutionLogRecord> store) => _store = store;

    public Task<Unit> HandleAsync(SaveWorkflowExecutionLog command, CancellationToken cancellationToken)
    {
        _store.SaveMany(command.Records);

        return Task.FromResult(Unit.Instance);
    }
}