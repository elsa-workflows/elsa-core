using Elsa.Management.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Commands;

namespace Elsa.Management.Services;

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