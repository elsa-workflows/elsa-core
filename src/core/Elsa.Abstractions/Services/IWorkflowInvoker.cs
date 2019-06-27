using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Serialization.Models;
using Elsa.Services.Models;

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
            WorkflowDefinition workflowDefinition,
            Variables input = null,
            WorkflowInstance workflowInstance = null,
            IEnumerable<string> startActivityIds = default,
            CancellationToken cancellationToken = default
        );
        
        Task<WorkflowExecutionContext> InvokeAsync<T>(
            WorkflowInstance workflowInstance = null,
            Variables input = null,
            IEnumerable<string> startActivityIds = default,
            CancellationToken cancellationToken = default
        ) where T:IWorkflow, new();

        Task<WorkflowExecutionContext> InvokeAsync(
            WorkflowInstance workflowInstance,
            Variables input = null,
            IEnumerable<string> startActivityIds = default,
            CancellationToken cancellationToken = default
        );
    }
}