using Elsa.Mediator.Models;
using Elsa.Mediator.Services;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Handlers;

// ReSharper disable once UnusedType.Global
internal class DispatchWorkflowDefinitionHandler : ICommandHandler<DispatchWorkflowDefinitionCommand>
{
    private readonly IWorkflowRuntime _workflowRuntime;

    public DispatchWorkflowDefinitionHandler(IWorkflowRuntime workflowRuntime)
    {
        _workflowRuntime = workflowRuntime;
    }
    
    public async Task<Unit> HandleAsync(DispatchWorkflowDefinitionCommand command, CancellationToken cancellationToken)
    {
        var options = new StartWorkflowRuntimeOptions(command.CorrelationId, command.Input, command.VersionOptions);
            
        await _workflowRuntime.StartWorkflowAsync(command.DefinitionId, options, cancellationToken);
        
        return Unit.Instance;
    }
}