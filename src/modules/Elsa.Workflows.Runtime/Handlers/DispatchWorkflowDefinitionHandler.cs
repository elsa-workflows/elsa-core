using Elsa.Mediator.Models;
using Elsa.Mediator.Services;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Handlers;

public class DispatchWorkflowDefinitionHandler : ICommandHandler<DispatchWorkflowDefinition>
{
    private readonly IWorkflowRuntime _workflowRuntime;

    public DispatchWorkflowDefinitionHandler(IWorkflowRuntime workflowRuntime)
    {
        _workflowRuntime = workflowRuntime;
    }
    
    public async Task<Unit> HandleAsync(DispatchWorkflowDefinition command, CancellationToken cancellationToken)
    {
        var options = new StartWorkflowOptions(command.CorrelationId, command.Input, command.VersionOptions);
            
        await _workflowRuntime.StartWorkflowAsync(command.DefinitionId, options, cancellationToken);
        
        return Unit.Instance;
    }
}