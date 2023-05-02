using Elsa.Common.Contracts;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// A default implementation that asynchronously stores the workflow state in a database using <see cref="IWorkflowInstanceStore"/>.
/// </summary>
public class AsyncWorkflowStateExporter : IWorkflowStateExporter, ICommandHandler<ExportWorkflowStateToDbCommand>
{
    private readonly IBackgroundCommandSender _backgroundCommandSender;
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly ISystemClock _systemClock;

    /// <summary>
    /// Constructor.
    /// </summary>
    public AsyncWorkflowStateExporter(
        IBackgroundCommandSender backgroundCommandSender,
        IWorkflowDefinitionStore workflowDefinitionStore,
        IWorkflowInstanceStore workflowInstanceStore,
        ISystemClock systemClock
    )
    {
        _backgroundCommandSender = backgroundCommandSender;
        _workflowDefinitionStore = workflowDefinitionStore;
        _workflowInstanceStore = workflowInstanceStore;
        _systemClock = systemClock;
    }

    /// <inheritdoc />
    public async ValueTask ExportAsync(Workflow workflow, WorkflowState workflowState, CancellationToken cancellationToken) =>
        await _backgroundCommandSender.SendAsync(new ExportWorkflowStateToDbCommand(workflowState), cancellationToken);

    /// <inheritdoc />
    public async Task<Unit> HandleAsync(ExportWorkflowStateToDbCommand command, CancellationToken cancellationToken)
    {
        var workflowState = command.WorkflowState;
        var definitionId = workflowState.DefinitionId;
        var version = workflowState.DefinitionVersion;
        var versionOptions = VersionOptions.SpecificVersion(version);
        var definitionFilter = new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = versionOptions };
        var definition = await _workflowDefinitionStore.FindAsync(definitionFilter, cancellationToken);

        if (definition == null)
            throw new Exception(
                $"Can't find workflow definition with definition ID {definitionId} and version {version}");

        var instanceFilter = new WorkflowInstanceFilter { Id = workflowState.Id };
        var workflowInstance = await _workflowInstanceStore.FindAsync(instanceFilter, cancellationToken);
        var now = _systemClock.UtcNow;

        workflowInstance ??= new WorkflowInstance
        {
            Id = workflowState.Id,
            CreatedAt = now
        };

        workflowInstance.DefinitionId = workflowState.DefinitionId;
        workflowInstance.DefinitionVersionId = definition.Id;
        workflowInstance.Version = workflowState.DefinitionVersion;
        workflowInstance.Status = workflowState.Status;
        workflowInstance.SubStatus = workflowState.SubStatus;
        workflowInstance.CorrelationId = workflowState.CorrelationId;
        workflowInstance.LastExecutedAt = now;
        workflowInstance.WorkflowState = workflowState;

        if (workflowState.Properties.TryGetValue<string>(SetName.WorkflowInstanceNameKey, out var name))
            workflowInstance.Name = name;

        // TODO: Store timestamps such as CancelledAt, FaultedAt, etc.

        await _workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
        return Unit.Instance;
    }
}