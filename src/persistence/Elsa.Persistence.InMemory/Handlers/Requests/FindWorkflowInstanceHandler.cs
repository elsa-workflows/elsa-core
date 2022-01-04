using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Services;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.InMemory.Handlers.Requests;

public class FindWorkflowInstanceHandler : IRequestHandler<FindWorkflowInstance, WorkflowInstance?>
{
    private readonly InMemoryStore<WorkflowInstance> _store;
    public FindWorkflowInstanceHandler(InMemoryStore<WorkflowInstance> store) => _store = store;

    public Task<WorkflowInstance?> HandleAsync(FindWorkflowInstance request, CancellationToken cancellationToken)
    {
        var instance = _store.Find(x => x.Id == request.InstanceId);

        return Task.FromResult(instance);
    }
}