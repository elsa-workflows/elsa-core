using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Serialization.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Core.Extensions
{
    public static class WorkflowInvokerExtensions
    {
        public static Task<WorkflowExecutionContext> ResumeAsync(this IWorkflowInvoker workflowInvoker,
            Workflow workflow,
            IEnumerable<IActivity> startActivities = default,
            CancellationToken cancellationToken = default)
        {
            workflow.Status = WorkflowStatus.Resuming;
            return workflowInvoker.InvokeAsync(workflow, startActivities, cancellationToken);
        }
        
        public static Task<WorkflowExecutionContext> ResumeAsync<T>(this IWorkflowInvoker workflowInvoker, 
            WorkflowInstance workflowInstance,
            Variables input = null,
            IEnumerable<string> startActivityIds = default, 
            CancellationToken cancellationToken = default) where T : IWorkflow, new()
        {
            workflowInstance.Status = WorkflowStatus.Resuming;
            return workflowInvoker.InvokeAsync(workflowInstance, input, startActivityIds, cancellationToken);
        }

        public static Task<WorkflowExecutionContext> ResumeAsync(this IWorkflowInvoker workflowInvoker, 
            WorkflowInstance workflowInstance, 
            Variables input = null, 
            IEnumerable<string> startActivityIds = default,
            CancellationToken cancellationToken = default)
        {
            workflowInstance.Status = WorkflowStatus.Resuming;
            return workflowInvoker.InvokeAsync(workflowInstance, input, startActivityIds, cancellationToken);
        }
    }
}