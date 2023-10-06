// using Elsa.Workflows.Core.Activities;
// using Elsa.Workflows.Core.Models;
// using Elsa.Workflows.Core.Services;
// using Elsa.Workflows.Core.State;
//
// namespace Elsa.Workflows.Core.Contracts;
//
// /// <summary>
// /// Creates a <see cref="WorkflowExecutionContext"/> for a workflow instance.
// /// </summary>
// public interface IWorkflowExecutionContextFactory
// {
//     /// <summary>
//     /// Creates a <see cref="WorkflowExecutionContext"/> for a workflow instance.
//     /// </summary>
//     Task<WorkflowExecutionContext> CreateAsync(
//         IServiceProvider serviceProvider,
//         Workflow workflow,
//         string instanceId,
//         WorkflowState? workflowState,
//         IDictionary<string, object>? input = default,
//         string? correlationId = default,
//         ExecuteActivityDelegate? executeActivityDelegate = default,
//         string? triggerActivityId = default,
//         CancellationTokens cancellationTokens = default);
// }