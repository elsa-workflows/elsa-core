using Elsa.Common.Contracts;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime.Commands;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// A default implementation that asynchronously stores the workflow state in a database using <see cref="IWorkflowInstanceStore"/>.
/// </summary>
internal class ExportWorkflowStateToDbCommandHandler : ICommandHandler<ExportWorkflowStateToDbCommand>
{
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly INotificationSender _notificationSender;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ExportWorkflowStateToDbCommandHandler(
        IWorkflowDefinitionStore workflowDefinitionStore,
        IWorkflowInstanceStore workflowInstanceStore,
        INotificationSender notificationSender
    )
    {
        _workflowDefinitionStore = workflowDefinitionStore;
        _workflowInstanceStore = workflowInstanceStore;
        _notificationSender = notificationSender;
    }

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

        workflowInstance ??= new WorkflowInstance
        {
            Id = workflowState.Id,
            CreatedAt = workflowState.CreatedAt
        };

        workflowInstance.DefinitionId = workflowState.DefinitionId;
        workflowInstance.DefinitionVersionId = definition.Id;
        workflowInstance.Version = workflowState.DefinitionVersion;
        workflowInstance.Status = workflowState.Status;
        workflowInstance.SubStatus = workflowState.SubStatus;
        workflowInstance.CorrelationId = workflowState.CorrelationId;
        workflowInstance.UpdatedAt = workflowState.UpdatedAt;
        workflowInstance.FinishedAt = workflowState.FinishedAt;
        workflowInstance.WorkflowState = workflowState;

        if (workflowState.Properties.TryGetValue<string>(SetName.WorkflowInstanceNameKey, out var name))
            workflowInstance.Name = name;

        await _workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
        await _notificationSender.SendAsync(new WorkflowInstanceSaved(workflowInstance), cancellationToken);
        return Unit.Instance;
    }
}