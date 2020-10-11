using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowHost
    {
        ValueTask<WorkflowExecutionContext> RunWorkflowInstanceAsync(
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default);
        
        ValueTask<WorkflowExecutionContext> RunWorkflowInstanceAsync(
            WorkflowDefinition workflowDefinition,
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default);

        ValueTask<WorkflowExecutionContext> RunWorkflowDefinitionAsync(
            WorkflowDefinition workflowDefinition,
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default);
    }
}