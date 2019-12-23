using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowRunner
    {
        Task<WorkflowExecutionContext> RunAsync(
            Workflow workflow,
            IEnumerable<IActivity>? startActivities = default,
            CancellationToken cancellationToken = default
        );
        
        Task<WorkflowExecutionContext> RunAsync<T>(
            Variable input = default,
            IEnumerable<string>? startActivityIds = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default) where T : IWorkflow, new();
        
        Task<WorkflowExecutionContext> RunAsync(
            WorkflowBlueprint workflowDefinition,
            Variable? input = default,
            IEnumerable<string>? startActivityIds = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default
        );
        
        Task<WorkflowExecutionContext> RunAsync(
            WorkflowDefinitionVersion workflowDefinition,
            Variable? input = default,
            IEnumerable<string> startActivityIds = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default
        );

        Task<WorkflowExecutionContext> RunAsync(
            WorkflowInstance workflowInstance,
            Variable? input = null,
            IEnumerable<string>? startActivityIds = default,
            CancellationToken cancellationToken = default);

        Task<WorkflowExecutionContext> ResumeAsync(
            Workflow workflow,
            IEnumerable<IActivity>? startActivities = default,
            CancellationToken cancellationToken = default
        );

        Task<WorkflowExecutionContext> ResumeAsync<T>(
            WorkflowInstance workflowInstance,
            Variable? input = default,
            IEnumerable<string>? startActivityIds = default,
            CancellationToken cancellationToken = default
        ) where T : IWorkflow, new();

        Task<WorkflowExecutionContext> ResumeAsync(
            WorkflowInstance workflowInstance,
            Variable? input = default,
            IEnumerable<string>? startActivityIds = default,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Starts new workflows that start with the specified activity name and resumes halted workflows that are blocked on activities with the specified activity name.
        /// </summary>
        Task<IEnumerable<WorkflowExecutionContext>> TriggerAsync(
            string activityType,
            Variable? input = default,
            string? correlationId = default,
            Func<Variables, bool>? activityStatePredicate = default,
            CancellationToken cancellationToken = default);
    }
}