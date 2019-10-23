using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Elsa.Services
{
    public class WorkflowInvoker : IWorkflowInvoker
    {
        private readonly IServiceProvider serviceProvider;

        public WorkflowInvoker(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public Task<WorkflowExecutionContext> StartAsync(
            Workflow workflow,
            IEnumerable<IActivity> startActivities = default,
            CancellationToken cancellationToken = default)
        {
            return Invoke(x => x.StartAsync(workflow, startActivities, cancellationToken));
        }

        public Task<WorkflowExecutionContext> StartAsync<T>(
            Variables input = default,
            IEnumerable<string> startActivityIds = default,
            string correlationId = default,
            CancellationToken cancellationToken = default) where T : IWorkflow, new()
        {
            return Invoke(x => x.StartAsync<T>(input, startActivityIds, correlationId, cancellationToken));
        }

        public Task<WorkflowExecutionContext> StartAsync(
            WorkflowDefinitionVersion workflowDefinition,
            Variables input = default,
            IEnumerable<string> startActivityIds = default,
            string correlationId = default,
            CancellationToken cancellationToken = default)
        {
            return Invoke(
                x => x.StartAsync(workflowDefinition, input, startActivityIds, correlationId, cancellationToken)
            );
        }

        public Task<WorkflowExecutionContext> ResumeAsync(
            Workflow workflow,
            IEnumerable<IActivity> startActivities = default,
            CancellationToken cancellationToken = default)
        {
            return Invoke(x => x.ResumeAsync(workflow, startActivities, cancellationToken));
        }

        public Task<WorkflowExecutionContext> ResumeAsync<T>(
            WorkflowInstance workflowInstance,
            Variables input = default,
            IEnumerable<string> startActivityIds = default,
            CancellationToken cancellationToken = default)
            where T : IWorkflow, new()
        {
            return Invoke(x => x.ResumeAsync<T>(workflowInstance, input, startActivityIds, cancellationToken));
        }

        public Task<WorkflowExecutionContext> ResumeAsync(
            WorkflowInstance workflowInstance,
            Variables input = default,
            IEnumerable<string> startActivityIds = default,
            CancellationToken cancellationToken = default)
        {
            return Invoke(x => x.ResumeAsync(workflowInstance, input, startActivityIds, cancellationToken));
        }

        public Task<IEnumerable<WorkflowExecutionContext>> TriggerAsync(
            string activityType,
            Variables input = default,
            string correlationId = default,
            Func<JObject, bool> activityStatePredicate = default,
            CancellationToken cancellationToken = default)
        {
            return Invoke(x => x.TriggerAsync(activityType, input, correlationId, activityStatePredicate, cancellationToken));
        }

        private async Task<T> Invoke<T>(Func<IScopedWorkflowInvoker, Task<T>> action)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var invoker = scope.ServiceProvider.GetRequiredService<IScopedWorkflowInvoker>();
                return await action(invoker);
            }
        }
    }
}