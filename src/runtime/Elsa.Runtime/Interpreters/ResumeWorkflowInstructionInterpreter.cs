using Elsa.Mediator.Services;
using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;
using Elsa.Runtime.Abstractions;
using Elsa.Runtime.Models;
using Elsa.Runtime.Services;
using Elsa.Services;
using Microsoft.Extensions.Logging;

namespace Elsa.Runtime.Interpreters;

public record ResumeWorkflowInstruction(WorkflowBookmark WorkflowBookmark, IDictionary<string, object>? Input, string? CorrelationId) : IWorkflowInstruction;

public class ResumeWorkflowInstructionInterpreter : WorkflowInstructionInterpreter<ResumeWorkflowInstruction>
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IWorkflowRegistry _workflowRegistry;
    private readonly IRequestSender _mediator;
    private readonly ILogger _logger;

    public ResumeWorkflowInstructionInterpreter(IWorkflowRunner workflowRunner, IWorkflowRegistry workflowRegistry, IRequestSender mediator, ILogger<ResumeWorkflowInstructionInterpreter> logger)
    {
        _workflowRunner = workflowRunner;
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
                    "Workflow bookmark {WorkflowBookmarkId} for workflow definition {WorkflowDefinitionId} references workflow instance ID {WorkflowInstanceId}, but no such workflow instance was found", workflowBookmark.Id, workflowBookmark.WorkflowDefinitionId, workflowBookmark.WorkflowInstanceId);

            return null;
        }

        var workflow = await _workflowRegistry.FindByIdAsync(workflowDefinitionId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);

        if (workflow == null)
        {
            _logger.LogWarning("Workflow bookmark {WorkflowBookmarkId} references workflow definition ID {WorkflowDefinitionId}, but no such workflow definition was found", workflowBookmark.Id, workflowBookmark.WorkflowDefinitionId);
            return null;
        }

        // Resume workflow instance.
        var bookmark = new Bookmark(workflowBookmark.Id, workflowBookmark.Name, workflowBookmark.Hash, workflowBookmark.Data, workflowBookmark.ActivityId, workflowBookmark.ActivityInstanceId, workflowBookmark.CallbackMethodName);
        var workflowState = workflowInstance.WorkflowState;
        var input = instruction.Input;
        var workflowExecutionResult = await _workflowRunner.RunAsync(workflow, workflowState, bookmark, input, cancellationToken);

        // Update workflow instance with new workflow state.
        workflowInstance.WorkflowState = workflowExecutionResult.WorkflowState;

        return new ExecuteWorkflowInstructionResult(workflow, workflowExecutionResult);
    }
}