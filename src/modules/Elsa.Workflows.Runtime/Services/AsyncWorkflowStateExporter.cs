using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// A default implementation that asynchronously stores the workflow state in a database using <see cref="IWorkflowInstanceStore"/>.
/// </summary>
public class AsyncWorkflowStateExporter : IWorkflowStateExporter
{
    private readonly IBackgroundCommandSender _backgroundCommandSender;

    /// <summary>
    /// Constructor.
    /// </summary>
    public AsyncWorkflowStateExporter(IBackgroundCommandSender backgroundCommandSender)
    {
        _backgroundCommandSender = backgroundCommandSender;
    }

    /// <inheritdoc />
    public async ValueTask ExportAsync(Workflow workflow, WorkflowState workflowState, CancellationToken cancellationToken) =>
        await _backgroundCommandSender.SendAsync(new ExportWorkflowStateToDbCommand(workflowState), cancellationToken);
}