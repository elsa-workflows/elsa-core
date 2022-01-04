using Elsa.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Models;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;
using Elsa.Runtime.Abstractions;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Instructions;
using Microsoft.Extensions.Logging;

namespace Elsa.Runtime.Interpreters;

public class ResumeWorkflowInstructionInterpreter : WorkflowInstructionInterpreter<ResumeWorkflowInstruction>
{
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowRegistry _workflowRegistry;
    private readonly IRequestSender _mediator;
    private readonly ILogger _logger;

    public ResumeWorkflowInstructionInterpreter(IWorkflowEngine workflowEngine, IWorkflowRegistry workflowRegistry, IRequestSender mediator, ILogger<ResumeWorkflowInstructionInterpreter> logger)
    {
        _workflowEngine = workflowEngine;
        _workflowRegistry = workflowRegistry;
        _mediator = mediator;
        _logger = logger;
    }

    protected override async ValueTask<ExecuteWorkflowInstructionResult?> ExecuteInstructionAsync(ResumeWorkflowInstruction instruction, CancellationToken cancellationToken = default)
    {
        var workflowBookmark = instruction.WorkflowBookmark;
        var workflowDefinitionId = workflowBookmark.WorkflowDefinitionId;
        var workflowInstanceId = workflowBookmark.WorkflowInstanceId;
        var workflowInstance = await _mediator.RequestAsync(new FindWorkflowInstance(workflowInstanceId), cancellationToken);

        if (workflowInstance == null)
        {
            _logger
                .LogWarning(
                    "Workflow bookmark {WorkflowBookmarkId} for workflow definition {WorkflowDefinitionId} points to workflow instance ID {WorkflowInstanceId}, but no such workflow instance was found", workflowBookmark.Id, workflowBookmark.WorkflowDefinitionId, workflowBookmark.WorkflowInstanceId);

            return null;
        }

        var workflow = await _workflowRegistry.FindByIdAsync(workflowDefinitionId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);

        if (workflow == null)
        {
            _logger.LogWarning("Workflow bookmark {WorkflowBookmarkId} points to workflow definition ID {WorkflowDefinitionId}, but no such workflow definition was found", workflowBookmark.Id, workflowBookmark.WorkflowDefinitionId);
            return null;
        }

        // Resume workflow instance.
        var bookmark = new Bookmark(workflowBookmark.Id, workflowBookmark.Name, workflowBookmark.Hash, workflowBookmark.ActivityId, workflowBookmark.ActivityInstanceId, workflowBookmark.Data, workflowBookmark.CallbackMethodName);
        var workflowState = workflowInstance.WorkflowState;
        var workflowExecutionResult = await _workflowEngine.ExecuteAsync(workflow, workflowState, bookmark, cancellationToken);

        // Update workflow instance with new workflow state.
        workflowInstance.WorkflowState = workflowExecutionResult.WorkflowState;

        return new ExecuteWorkflowInstructionResult(workflow, workflowExecutionResult);
    }
}