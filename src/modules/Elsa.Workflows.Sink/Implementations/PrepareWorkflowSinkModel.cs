using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Workflows.Runtime.Services;
using Elsa.Workflows.Sink.Contracts;
using Elsa.Workflows.Sink.Models;

namespace Elsa.Workflows.Sink.Implementations;

public class PrepareWorkflowSinkModel : IPrepareWorkflowSinkModel
{
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowStateStore _workflowStateStore;
    private readonly ISystemClock _systemClock;
        
    public PrepareWorkflowSinkModel(
        IWorkflowDefinitionService workflowDefinitionService, 
        IWorkflowStateStore workflowStateStore,
        ISystemClock systemClock)
    {
        _workflowDefinitionService = workflowDefinitionService;
        _workflowStateStore = workflowStateStore;
        _systemClock = systemClock;
    }
        
    public async Task<WorkflowSinkDto> ExecuteAsync(string definitionId, int definitionVersion, string stateId, CancellationToken cancellationToken)
    {
        var workflowDefinition = await _workflowDefinitionService.FindAsync(definitionId, VersionOptions.SpecificVersion(definitionVersion), cancellationToken);
        if (workflowDefinition is null)
            throw new Exception(
                $"Can't find workflow definition with definition ID {definitionId} and version {definitionVersion}");

        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        
        var workflowState = await _workflowStateStore.LoadAsync(stateId, cancellationToken);
        if (workflowState is null)
            throw new Exception(
                $"Can't load workflow state with workflow state ID {stateId}");

        var now = _systemClock.UtcNow;

        var workflowSinkDto = new WorkflowSinkDto
        {
            Id = workflowState.Id,
            Workflow = workflow,
            WorkflowState = workflowState,
            CreatedAt = now,
            LastExecutedAt = now
        };
        return workflowSinkDto;
    }
}