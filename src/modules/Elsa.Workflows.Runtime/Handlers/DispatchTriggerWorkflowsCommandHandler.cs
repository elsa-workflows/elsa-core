using Elsa.Mediator.Models;
using Elsa.Mediator.Services;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Handlers;

// ReSharper disable once UnusedType.Global
internal class DispatchTriggerWorkflowsCommandHandler : ICommandHandler<DispatchTriggerWorkflowsCommand>
{
    private readonly IWorkflowRuntime _workflowRuntime;

    public DispatchTriggerWorkflowsCommandHandler(IWorkflowRuntime workflowRuntime)
    {
        _workflowRuntime = workflowRuntime;
    }

    public async Task<Unit> HandleAsync(DispatchTriggerWorkflowsCommand command, CancellationToken cancellationToken)
    {
        var options = new TriggerWorkflowsRuntimeOptions(command.CorrelationId, command.Input);
        await _workflowRuntime.TriggerWorkflowsAsync(command.ActivityTypeName, command.BookmarkPayload, options, cancellationToken);

        return Unit.Instance;
    }
}