using Elsa.Common.Services;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Services;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Implementations;

/// <summary>
/// A default implementation that stores the workflow state in a database using <see cref="IWorkflowInstanceStore"/>.
/// </summary>
public class StoreWorkflowStateExporter : IWorkflowStateExporter
{
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly ISystemClock _systemClock;
    
    public StoreWorkflowStateExporter(IWorkflowInstanceStore workflowInstanceStore, ISystemClock systemClock)
    {
        _workflowInstanceStore = workflowInstanceStore;
        _systemClock = systemClock;
    }
    
    public async ValueTask ExportAsync(WorkflowDefinition definition, WorkflowState workflowState, CancellationToken cancellationToken)
    {
        var workflowInstance = FromWorkflowState(workflowState, definition);
        await _workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
    }
    
    private WorkflowInstance FromWorkflowState(WorkflowState workflowState, WorkflowDefinition workflowDefinition)
    {
        var workflowInstance = new WorkflowInstance
        {
            Id = workflowState.Id,
            DefinitionId = workflowDefinition.DefinitionId,
            DefinitionVersionId = workflowDefinition.Id,
            Version = workflowDefinition.Version,
            WorkflowState = workflowState,
            Status = workflowState.Status,
            SubStatus = workflowState.SubStatus,
            CorrelationId = workflowState.CorrelationId,
            Name = null,
        };

        // Update timestamps.
        var now = _systemClock.UtcNow;

        if (workflowInstance.Status == WorkflowStatus.Finished)
        {
            switch (workflowInstance.SubStatus)
            {
                case WorkflowSubStatus.Cancelled:
                    workflowInstance.CancelledAt = now;
                    break;
                case WorkflowSubStatus.Faulted:
                    workflowInstance.FaultedAt = now;
                    break;
                case WorkflowSubStatus.Finished:
                    workflowInstance.FinishedAt = now;
                    break;
            }
        }

        return workflowInstance;
    }
}