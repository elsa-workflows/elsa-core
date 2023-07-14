using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// An implementation that stores the workflow state in a database using <see cref="IWorkflowInstanceStore"/> from a background worker.
/// </summary>
public class BackgroundWorkflowStateExporter : IWorkflowStateExporter
{
    private readonly ICommandSender _commandSender;

    /// <summary>
    /// Constructor.
    /// </summary>
    public BackgroundWorkflowStateExporter(ICommandSender commandSender)
    {
        _commandSender = commandSender;
    }

    /// <inheritdoc />
    public async ValueTask ExportAsync(Workflow workflow, WorkflowState workflowState, CancellationToken cancellationToken) =>
        await _commandSender.SendAsync(new ExportWorkflowStateToDbCommand(workflowState), CommandStrategy.Background, cancellationToken);
}