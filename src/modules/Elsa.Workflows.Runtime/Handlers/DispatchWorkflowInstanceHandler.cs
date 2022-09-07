using Elsa.Mediator.Models;
using Elsa.Mediator.Services;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Handlers;

public class DispatchWorkflowInstanceHandler : ICommandHandler<DispatchWorkflowInstance>
{
    private readonly IWorkflowRuntime _workflowRuntime;

    public DispatchWorkflowInstanceHandler(IWorkflowRuntime workflowRuntime)
    {
        _workflowRuntime = workflowRuntime;
    }
    
    public async Task<Unit> HandleAsync(DispatchWorkflowInstance command, CancellationToken cancellationToken)
    {
        var options = new ResumeWorkflowOptions(command.Input);
        await _workflowRuntime.ResumeWorkflowAsync(command.InstanceId, command.BookmarkId, options, cancellationToken);
        
        return Unit.Instance;
    }
}