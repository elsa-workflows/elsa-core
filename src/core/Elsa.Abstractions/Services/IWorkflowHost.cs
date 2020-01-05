using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowHost
    {
        /// <summary>
        /// Run a registered workflow by its ID.
        /// </summary>
        Task RunAsync(string workflowId, object? input = default, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Starts new workflows that start with the specified activity name and resumes halted workflows that are blocked on activities with the specified activity name.
        /// </summary>
        Task TriggerAsync(
            string activityType,
            object? input = default,
            string? correlationId = default,
            Func<Variables, bool>? activityStatePredicate = default,
            CancellationToken cancellationToken = default);

        // Task<WorkflowExecutionContext> RunAsync(
        //     ProcessInstance processInstance,
        //     IEnumerable<IActivity>? startActivities = default,
        //     CancellationToken cancellationToken = default
        // );
        //
        // Task<WorkflowExecutionContext> RunAsync(
        //     Workflow workflow,
        //     Variable? input = default,
        //     IEnumerable<string>? startActivityIds = default,
        //     string? correlationId = default,
        //     CancellationToken cancellationToken = default
        // );
        //
        // Task<WorkflowExecutionContext> RunAsync(
        //     ProcessDefinitionVersion processDefinition,
        //     Variable? input = default,
        //     IEnumerable<string> startActivityIds = default,
        //     string? correlationId = default,
        //     CancellationToken cancellationToken = default
        // );
        //
        // Task<WorkflowExecutionContext> RunAsync(
        //     ProcessInstance processInstance,
        //     Variable? input = null,
        //     IEnumerable<string>? startActivityIds = default,
        //     CancellationToken cancellationToken = default);
        //
        // Task<WorkflowExecutionContext> ResumeAsync(
        //     ProcessInstance processInstance,
        //     IEnumerable<IActivity>? startActivities = default,
        //     CancellationToken cancellationToken = default
        // );
        //
        // Task<WorkflowExecutionContext> ResumeAsync(
        //     ProcessInstance processInstance,
        //     Variable? input = default,
        //     IEnumerable<string>? startActivityIds = default,
        //     CancellationToken cancellationToken = default
        // );
    }
}