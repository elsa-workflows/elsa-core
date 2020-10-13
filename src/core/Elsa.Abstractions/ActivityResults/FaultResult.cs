using Elsa.Services.Models;
using Microsoft.Extensions.Localization;

namespace Elsa.ActivityResults
{
    public class FaultResult : ActivityExecutionResult
    {
        public FaultResult(LocalizedString message) => Message = message;
        public LocalizedString Message { get; }
        
        protected override void Execute(ActivityExecutionContext activityExecutionContext) => 
            activityExecutionContext.WorkflowExecutionContext.Fault(activityExecutionContext.ActivityBlueprint.Id, Message);
    }
}