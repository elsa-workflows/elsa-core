using Elsa.Mediator.Models;
using Elsa.Mediator.Services;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Handlers;

// ReSharper disable once UnusedType.Global
internal class DispatchWorkflowInstanceHandler : ICommandHandler<DispatchWorkflowInstanceCommand>
{
    private readonly IWorkflowRuntime _workflowRuntime;

    public DispatchWorkflowInstanceHandler(IWorkflowRuntime workflowRuntime)
    {
        _workflowRuntime = workflowRuntime;
    }

    public async Task<Unit> HandleAsync(DispatchWorkflowInstanceCommand command, CancellationToken cancellationToken)
    {
        var options = new ResumeWorkflowRuntimeOptions(command.CorrelationId, command.BookmarkId, command.ActivityId, command.Input);
        await _workflowRuntime.ResumeWorkflowAsync(command.InstanceId, options, cancellationToken);

        return Unit.Instance;
    }
}