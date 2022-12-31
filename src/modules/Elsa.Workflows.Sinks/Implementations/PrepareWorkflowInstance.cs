using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Services;
using Elsa.Workflows.Sinks.Contracts;
using Elsa.Workflows.Sinks.Models;

namespace Elsa.Workflows.Sinks.Implementations;

public class PrepareWorkflowInstance : IPrepareWorkflowInstance
{
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly ISystemClock _systemClock;
        
    public PrepareWorkflowInstance(
        IWorkflowDefinitionService workflowDefinitionService,
        ISystemClock systemClock)
    {
        _workflowDefinitionService = workflowDefinitionService;
        _systemClock = systemClock;
    }
        
    public async Task<WorkflowInstanceDto> ExecuteAsync(WorkflowState state, CancellationToken cancellationToken)
    {
        var workflowDefinition = await _workflowDefinitionService.FindAsync(state.DefinitionId, VersionOptions.SpecificVersion(state.DefinitionVersion), cancellationToken);
        if (workflowDefinition is null)
            throw new Exception(
                $"Can't find workflow definition with definition ID {state.DefinitionId} and version {state.DefinitionVersion}");

        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);

        var now = _systemClock.UtcNow;

        var workflowSinkDto = new WorkflowInstanceDto
        {
            Id = state.Id,
            Workflow = workflow,
            WorkflowState = state,
            CreatedAt = now,
            LastExecutedAt = now
        };
        return workflowSinkDto;
    }
}