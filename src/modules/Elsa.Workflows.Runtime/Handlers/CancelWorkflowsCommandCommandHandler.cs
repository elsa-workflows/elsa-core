using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Handles the <see cref="DispatchWorkflowDefinitionCommand"/>.
/// </summary>
public class CancelWorkflowsCommandHandler(IWorkflowCancellationService workflowCancellationService) : ICommandHandler<CancelWorkflowsCommand>
{
    /// <inheritdoc />
    public async Task<Unit> HandleAsync(CancelWorkflowsCommand command, CancellationToken cancellationToken)
    {
        var tasks = new List<Task<int>>();
        
        if (command.Request.WorkflowInstanceIds is not null)
            tasks.Add(workflowCancellationService.CancelWorkflowsAsync(command.Request.WorkflowInstanceIds!, cancellationToken));
        if (command.Request.DefinitionVersionId is not null)
            tasks.Add(workflowCancellationService.CancelWorkflowByDefinitionVersionAsync(command.Request.DefinitionVersionId!, cancellationToken));
        if (command.Request.DefinitionId is not null)
            tasks.Add(workflowCancellationService.CancelWorkflowByDefinitionAsync(command.Request.DefinitionId!, command.Request.VersionOptions!.Value, cancellationToken));

        await Task.WhenAll(tasks);

        return Unit.Instance;
    }
}