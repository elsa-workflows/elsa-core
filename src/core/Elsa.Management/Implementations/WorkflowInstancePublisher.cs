using Elsa.Management.Services;
using Elsa.Mediator.Services;
using Elsa.Persistence.Commands;

namespace Elsa.Management.Implementations;

public class WorkflowInstancePublisher : IWorkflowInstancePublisher
{
    private readonly IMediator _mediator;

    public WorkflowInstancePublisher(IMediator mediator)
    {
        _mediator = mediator;
    }
    public async Task DeleteAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        await _mediator.ExecuteAsync(new DeleteWorkflowInstance(instanceId),
            cancellationToken);
    }

    public async Task BulkDeleteAsync(IEnumerable<string> instanceIds, CancellationToken cancellationToken = default)
    {
        await _mediator.ExecuteAsync(new DeleteWorkflowInstances(instanceIds), cancellationToken);
    }
}