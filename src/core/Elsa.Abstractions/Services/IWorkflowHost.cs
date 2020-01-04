using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;
using ProcessInstance = Elsa.Models.ProcessInstance;

namespace Elsa.Services
{
    using ProcessInstance = Elsa.Services.Models.ProcessInstance;
    
    public interface IWorkflowHost
    {
        Task<WorkflowExecutionContext> RunAsync(string workflowId, object? input = default, CancellationToken cancellationToken = default);

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
        //
        // /// <summary>
        // /// Starts new workflows that start with the specified activity name and resumes halted workflows that are blocked on activities with the specified activity name.
        // /// </summary>
        // Task<IEnumerable<WorkflowExecutionContext>> TriggerAsync(
        //     string activityType,
        //     Variable? input = default,
        //     string? correlationId = default,
        //     Func<Variables, bool>? activityStatePredicate = default,
        //     CancellationToken cancellationToken = default);
    }
}