using Elsa.Mediator.Contracts;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Contracts;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Commands;

public class SaveWorkflowExecutionLogHandler : ICommandHandler<SaveWorkflowExecutionLog>
{
    private readonly IStore<WorkflowExecutionLogRecord> _store;
    public SaveWorkflowExecutionLogHandler(IStore<WorkflowExecutionLogRecord> store) => _store = store;

    public async Task<Unit> HandleAsync(SaveWorkflowExecutionLog command, CancellationToken cancellationToken)
    {
        await _store.SaveManyAsync(command.Records, cancellationToken);

        return Unit.Instance;
    }
}