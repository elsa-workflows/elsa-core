using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Models;
using Elsa.Workflows.Persistence.Services;
using Elsa.Workflows.Runtime.Abstractions;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Interpreters;

public record ResumeWorkflowInstruction(WorkflowBookmark WorkflowBookmark, IDictionary<string, object>? Input, string? CorrelationId) : IWorkflowInstruction;

public class ResumeWorkflowInstructionInterpreter : WorkflowInstructionInterpreter<ResumeWorkflowInstruction>
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly ILogger _logger;

    public ResumeWorkflowInstructionInterpreter(
        IWorkflowRunner workflowRunner, 
        IWorkflowDefinitionStore workflowDefinitionStore, 
        IWorkflowInstanceStore workflowInstanceStore,
        IWorkflowDefinitionService workflowDefinitionService,
        ILogger<ResumeWorkflowInstructionInterpreter> logger)
    {
        _workflowRunner = workflowRunner;
        _workflowDefinitionStore = workflowDefinitionStore;
        _workflowInstanceStore = workflowInstanceStore;
        _workflowDefinitionService = workflowDefinitionService;
        _logger = logger;
    }

    protected override async ValueTask<ExecuteWorkflowInstructionResult?> ExecuteInstructionAsync(ResumeWorkflowInstruction instruction, CancellationToken cancellationToken = default)
    {
        var workflowBookmark = instruction.WorkflowBookmark;
        var workflowDefinitionId = workflowBookmark.WorkflowDefinitionId;
        var workflowInstanceId = workflowBookmark.WorkflowInstanceId;
        var workflowInstance = await _workflowInstanceStore.FindByIdAsync(workflowInstanceId, cancellationToken);

        if (workflowInstance == null)
        {
            _logger
                .LogWarning(
                    "Workflow bookmark {WorkflowBookmarkId} for workflow definition {WorkflowDefinitionId} references workflow instance ID {WorkflowInstanceId}, but no such workflow instance was found", workflowBookmark.Id, workflowBookmark.WorkflowDefinitionId, workflowBookmark.WorkflowInstanceId);

            return null;
        }

        var definition = await _workflowDefinitionStore.FindByDefinitionIdAsync(workflowDefinitionId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);

        if (definition == null)
        {
            _logger.LogWarning("Workflow bookmark {WorkflowBookmarkId} references workflow definition ID {WorkflowDefinitionId}, but no such workflow definition was found", workflowBookmark.Id, workflowBookmark.WorkflowDefinitionId);
            return null;
        }

        // Resume workflow instance.
        var bookmark = new Bookmark(workflowBookmark.Id, workflowBookmark.Name, workflowBookmark.Hash, workflowBookmark.Data, workflowBookmark.ActivityId, workflowBookmark.ActivityInstanceId, workflowBookmark.CallbackMethodName);
        var workflowState = workflowInstance.WorkflowState;
        var input = instruction.Input;
        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
        var workflowExecutionResult = await _workflowRunner.RunAsync(workflow, workflowState, bookmark, input, cancellationToken);

        // Update workflow instance with new workflow state.
        workflowInstance.WorkflowState = workflowExecutionResult.WorkflowState;

        return new ExecuteWorkflowInstructionResult(workflowExecutionResult);
    }
}