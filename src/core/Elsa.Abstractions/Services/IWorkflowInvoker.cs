using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.Services
{
    public interface IWorkflowInvoker
    {
        Task<WorkflowExecutionContext> InvokeAsync(
            Workflow workflow,
            IEnumerable<IActivity> startActivityIds = default,
            CancellationToken cancellationToken = default
        );

        Task<WorkflowExecutionContext> InvokeAsync(
            WorkflowDefinitionVersion workflowDefinition,
            Variables input = default,
            WorkflowInstance workflowInstance = default,
            IEnumerable<string> startActivityIds = default,
            string correlationId = default,
            CancellationToken cancellationToken = default
        );

        Task<WorkflowExecutionContext> InvokeAsync<T>(
            WorkflowInstance workflowInstance = default,
            Variables input = default,
            IEnumerable<string> startActivityIds = default,
            CancellationToken cancellationToken = default
        ) where T : IWorkflow, new();

        Task<WorkflowExecutionContext> InvokeAsync(
            WorkflowInstance workflowInstance,
            Variables input = default,
            IEnumerable<string> startActivityIds = default,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Starts new workflows that start with the specified activity name and resumes halted workflows that are blocked on activities with the specified activity name.
        /// </summary>
        Task<IEnumerable<WorkflowExecutionContext>> TriggerAsync(string activityType,
            Variables input = default,
            string correlationId = default,
            Func<JObject, bool> activityStatePredicate = default,
            CancellationToken cancellationToken = default);
    }
}