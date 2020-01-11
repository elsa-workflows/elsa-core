using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Results
{
    public class CorrelateResult : IActivityExecutionResult
    {
        public CorrelateResult(string correlationId)
        {
            CorrelationId = correlationId;
        }
        
        public string CorrelationId { get; }
        
        public Task ExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            workflowExecutionContext.CorrelationId = CorrelationId;
            return Task.CompletedTask;
        }
    }
}