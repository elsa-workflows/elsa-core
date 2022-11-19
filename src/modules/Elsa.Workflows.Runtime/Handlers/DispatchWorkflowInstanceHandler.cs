using Elsa.Mediator.Models;
using Elsa.Mediator.Services;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Handlers;

internal class DispatchWorkflowInstanceHandler : ICommandHandler<DispatchWorkflowInstance>
{
    private readonly IWorkflowRuntime _workflowRuntime;

    public DispatchWorkflowInstanceHandler(IWorkflowRuntime workflowRuntime)
    {
        _workflowRuntime = workflowRuntime;
    }

    public async Task<Unit> HandleAsync(DispatchWorkflowInstance command, CancellationToken cancellationToken)
    {
        var options = new ResumeWorkflowRuntimeOptions(command.CorrelationId, command.BookmarkId, command.ActivityId, command.Input);
        await _workflowRuntime.ResumeWorkflowAsync(command.InstanceId, options, cancellationToken);

        return Unit.Instance;
    }
}