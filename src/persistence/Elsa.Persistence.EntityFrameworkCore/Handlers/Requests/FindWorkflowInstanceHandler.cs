using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Services;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Requests;

public class FindWorkflowInstanceHandler : IRequestHandler<FindWorkflowInstance, WorkflowInstance?>
{
    private readonly IStore<WorkflowInstance> _store;
    public FindWorkflowInstanceHandler(IStore<WorkflowInstance> store) => _store = store;

    public async Task<WorkflowInstance?> HandleAsync(FindWorkflowInstance request, CancellationToken cancellationToken) => await _store.FindAsync(x => x.Id == request.InstanceId, cancellationToken);
}