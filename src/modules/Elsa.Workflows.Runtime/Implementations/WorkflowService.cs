// using Elsa.Models;
// using Elsa.Workflows.Core.Models;
// using Elsa.Workflows.Core.Services;
// using Elsa.Workflows.Persistence.Entities;
// using Elsa.Workflows.Runtime.Models;
// using Elsa.Workflows.Runtime.Services;
//
// namespace Elsa.Workflows.Runtime.Implementations;
//
// public class WorkflowService : IWorkflowService
// {
//     private readonly IWorkflowInvoker _workflowInvoker;
//     private readonly IWorkflowDispatcher _workflowDispatcher;
//     private readonly IHasher _hasher;
//
//     public WorkflowService(
//         IWorkflowInvoker workflowInvoker,
//         IWorkflowDispatcher workflowDispatcher,
//         IHasher hasher)
//     {
//         _workflowInvoker = workflowInvoker;
//         _workflowDispatcher = workflowDispatcher;
//         _hasher = hasher;
//     }
//
//     public async Task<ExecuteWorkflowResult> ExecuteWorkflowAsync(string definitionId, VersionOptions versionOptions, IDictionary<string, object>? input = default, string? correlationId = default, CancellationToken cancellationToken = default)
//     {
//         var executeRequest = new InvokeWorkflowDefinitionRequest(definitionId, versionOptions, input, correlationId);
//         var result = await _workflowInvoker.InvokeAsync(executeRequest, cancellationToken);
//
//         return new ExecuteWorkflowResult(result.WorkflowState, result.Bookmarks);
//     }
//
//     public async Task<ExecuteWorkflowResult> ExecuteWorkflowAsync(string instanceId, Bookmark bookmark, IDictionary<string, object>? input = default, string? correlationId = default, CancellationToken cancellationToken = default)
//     {
//         var request = new InvokeWorkflowInstanceRequest(instanceId, bookmark, input, correlationId);
//         var result = await _workflowInvoker.InvokeAsync(request, cancellationToken);
//         return new ExecuteWorkflowResult(result.WorkflowState, result.Bookmarks);
//     }
//
//     public async Task<DispatchWorkflowDefinitionResponse> DispatchWorkflowAsync(string definitionId, VersionOptions versionOptions, IDictionary<string, object>? input = default, string? correlationId = default, CancellationToken cancellationToken = default)
//     {
//         var executeRequest = new DispatchWorkflowDefinitionRequest(definitionId, versionOptions, input, correlationId);
//         return await _workflowDispatcher.DispatchAsync(executeRequest, cancellationToken);
//     }
//
//     public async Task<DispatchWorkflowInstanceResponse> DispatchWorkflowAsync(string instanceId, Bookmark bookmark, IDictionary<string, object>? input = default, string? correlationId = default, CancellationToken cancellationToken = default)
//     {
//         var request = new DispatchWorkflowInstanceRequest(instanceId, bookmark, input, correlationId);
//         return await _workflowDispatcher.DispatchAsync(request, cancellationToken);
//     }
//     
//     protected override async ValueTask<ExecuteWorkflowInstructionResult?> ResumeWorkflowAsync(WorkflowBookmark workflowBookmark, CancellationToken cancellationToken = default)
//     {
//         // var workflowDefinitionId = workflowBookmark.WorkflowDefinitionId;
//         // var workflowInstanceId = workflowBookmark.WorkflowInstanceId;
//         // var workflowInstance = await _workflowInstanceStore.FindByIdAsync(workflowInstanceId, cancellationToken);
//         //
//         // if (workflowInstance == null)
//         // {
//         //     _logger
//         //         .LogWarning(
//         //             "Workflow bookmark {WorkflowBookmarkId} for workflow definition {WorkflowDefinitionId} references workflow instance ID {WorkflowInstanceId}, but no such workflow instance was found", workflowBookmark.Id, workflowBookmark.WorkflowDefinitionId, workflowBookmark.WorkflowInstanceId);
//         //
//         //     return null;
//         // }
//         //
//         // var definition = (await _workflowDefinitionStore.FindManyByDefinitionIdAsync(workflowDefinitionId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken)).FirstOrDefault();
//         //
//         // if (definition == null)
//         // {
//         //     _logger.LogWarning("Workflow bookmark {WorkflowBookmarkId} references workflow definition ID {WorkflowDefinitionId}, but no such workflow definition was found", workflowBookmark.Id, workflowBookmark.WorkflowDefinitionId);
//         //     return null;
//         // }
//         //
//         // // Resume workflow instance.
//         // var bookmark = new Bookmark(workflowBookmark.Id, workflowBookmark.Name, workflowBookmark.Hash, workflowBookmark.Data, workflowBookmark.ActivityId, workflowBookmark.ActivityInstanceId, workflowBookmark.CallbackMethodName);
//         // var workflowState = workflowInstance.WorkflowState;
//         // var input = instruction.Input;
//         // var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
//         // var workflowExecutionResult = await _workflowInvoker.InvokeAsync(workflow, workflowState, bookmark, input, cancellationToken);
//         //
//         // // Update workflow instance with new workflow state.
//         // workflowInstance.WorkflowState = workflowExecutionResult.WorkflowState;
//         //
//         // return new ExecuteWorkflowInstructionResult(workflowExecutionResult);
//     }
// }