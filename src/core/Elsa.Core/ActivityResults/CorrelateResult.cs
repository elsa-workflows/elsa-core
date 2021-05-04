using Elsa.Services.Models;

namespace Elsa.ActivityResults
{
    public class CorrelateResult : ActivityExecutionResult
    {
        public CorrelateResult(string correlationId) => CorrelationId = correlationId;
        public string CorrelationId { get; }
        
        protected override void Execute(ActivityExecutionContext activityExecutionContext) => activityExecutionContext.WorkflowExecutionContext.CorrelationId = CorrelationId;
    }
}