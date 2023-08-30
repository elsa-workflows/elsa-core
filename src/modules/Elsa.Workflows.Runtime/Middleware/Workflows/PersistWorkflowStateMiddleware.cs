// using Elsa.Common.Contracts;
// using Elsa.Workflows.Core;
// using Elsa.Workflows.Core.Contracts;
// using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
// using Elsa.Workflows.Management.Contracts;
// using Elsa.Workflows.Management.Mappers;
//
// namespace Elsa.Workflows.Runtime.Middleware.Workflows;
//
// /// <summary>
// /// Takes care of persisting workflow execution log entries.
// /// </summary>
// public class PersistWorkflowStateMiddleware : WorkflowExecutionMiddleware
// {
//     private readonly IWorkflowInstanceStore _workflowInstanceStore;
//     private readonly IWorkflowStateExtractor _workflowStateExtractor;
//     private readonly WorkflowStateMapper _workflowStateMapper;
//     private readonly ISystemClock _systemClock;
//
//     /// <inheritdoc />
//     public PersistWorkflowStateMiddleware(
//         WorkflowMiddlewareDelegate next, 
//         IWorkflowInstanceStore workflowInstanceStore, 
//         IWorkflowStateExtractor workflowStateExtractor,
//         WorkflowStateMapper workflowStateMapper,
//         ISystemClock systemClock) : base(next)
//     {
//         _workflowInstanceStore = workflowInstanceStore;
//         _workflowStateExtractor = workflowStateExtractor;
//         _workflowStateMapper = workflowStateMapper;
//         _systemClock = systemClock;
//     }
//
//     /// <inheritdoc />
//     public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
//     {
//         // Invoke next middleware.
//         await Next(context);
//
//         // Extract workflow state.
//         var workflowState = _workflowStateExtractor.Extract(context);
//         
//         // Create a workflow instance from the workflow state.
//         var workflowInstance = _workflowStateMapper.Map(workflowState);
//         
//         // Persist workflow state.
//         await _workflowInstanceStore.SaveAsync(workflowInstance, context.CancellationToken);
//     }
// }