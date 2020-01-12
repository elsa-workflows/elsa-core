using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;
using Microsoft.Extensions.Localization;

namespace Elsa.Results
{
    public class FaultResult : IActivityExecutionResult
    {
        public FaultResult(LocalizedString message)
        {
            Message = message;
        }
        
        public LocalizedString Message { get; }
        
        public Task ExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            workflowExecutionContext.Fault(activityExecutionContext.Activity, Message);
            return Task.CompletedTask;
        }
    }
}