// using Elsa.Models;
// using Elsa.Workflows.Core.Models;
// using Elsa.Workflows.Core.Services;
// using Elsa.Workflows.Core.State;
// using Elsa.Workflows.Persistence.Entities;
// using Elsa.Workflows.Persistence.Services;
// using Elsa.Workflows.Runtime.Models;
// using Elsa.Workflows.Runtime.Services;
//
// namespace Elsa.Workflows.Runtime.Implementations;
//
// /// <summary>
// /// A basic implementation that directly executes the specified workflow in local memory (as opposed elsewhere in some cluster).
// /// </summary>
// public class DefaultWorkflowInvoker : IWorkflowInvoker
// {
//     private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
//     private readonly IWorkflowInstanceStore _workflowInstanceStore;
//     private readonly IWorkflowDefinitionService _workflowDefinitionService;
//     private readonly IWorkflowRunner _workflowRunner;
//
//     public DefaultWorkflowInvoker(
//         IWorkflowDefinitionStore workflowDefinitionStore,
//         IWorkflowInstanceStore workflowInstanceStore,
//         IWorkflowDefinitionService workflowDefinitionService,
//         IWorkflowRunner workflowRunner)
//     {
//         _workflowInstanceStore = workflowInstanceStore;
//         _workflowDefinitionService = workflowDefinitionService;
//         _workflowRunner = workflowRunner;
//         _workflowDefinitionStore = workflowDefinitionStore;
//     }
//
//     public async Task<InvokeWorkflowResult> InvokeAsync(InvokeWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
//     {
//         var definition = await GetWorkflowDefinitionAsync(request.DefinitionId, request.VersionOptions, cancellationToken);
//         var input = request.Input;
//         var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
//         var result = await _workflowRunner.RunAsync(workflow, input, cancellationToken);
//         return new InvokeWorkflowResult(result.WorkflowState, result.Bookmarks);
//     }
//
//     public async Task<InvokeWorkflowResult> InvokeAsync(InvokeWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
//     {
//         var workflowInstance = await _workflowInstanceStore.FindByIdAsync(request.InstanceId, cancellationToken);
//
//         if (workflowInstance == null)
//             throw new Exception($"No workflow instance found with ID {request.InstanceId}");
//         
//         return await InvokeAsync(workflowInstance!, request.Bookmark, request.Input, cancellationToken);
//     }
//
//     public async Task<InvokeWorkflowResult> InvokeAsync(WorkflowInstance workflowInstance, Bookmark? bookmark = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
//     {
//         var workflow = (await _workflowDefinitionStore.FindManyByDefinitionIdAsync(workflowInstance.DefinitionId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken)).FirstOrDefault();
//
//         if (workflow == null)
//             throw new Exception($"Workflow instance references a workflow that does not exist");
//
//         var workflowState = workflowInstance.WorkflowState;
//         return await InvokeAsync(workflow, workflowState, bookmark, input, cancellationToken);
//     }
//
//     public async Task<InvokeWorkflowResult> InvokeAsync(WorkflowDefinition workflowDefinition, WorkflowState workflowState, Bookmark? bookmark = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
//     {
//         var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
//         return await InvokeAsync(workflow, workflowState, bookmark, input, cancellationToken);
//     }
//
//     public async Task<InvokeWorkflowResult> InvokeAsync(Workflow workflow, WorkflowState workflowState, Bookmark? bookmark = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
//     {
//         var result = await _workflowRunner.RunAsync(workflow, workflowState, bookmark, input, cancellationToken);
//         return new InvokeWorkflowResult(result.WorkflowState, result.Bookmarks);
//     }
//
//     public Task StartAsync(string definitionId, VersionOptions versionOptions, IDictionary<string, object>? input = default, string? correlationId = default, CancellationToken cancellationToken = default)
//     {
//         throw new NotImplementedException();
//     }
//
//     private async Task<WorkflowDefinition> GetWorkflowDefinitionAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
//     {
//         var definition = (await _workflowDefinitionStore.FindManyByDefinitionIdAsync(definitionId, versionOptions, cancellationToken)).FirstOrDefault();
//
//         if (definition == null)
//             throw new Exception($"Workflow instance references a workflow that does not exist");
//
//         return definition;
//     }
// }