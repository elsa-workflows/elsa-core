using System;
using Elsa.Services.Models;

namespace Elsa.ActivityResults
{
    public class FaultResult : ActivityExecutionResult
    {
        public FaultResult(Exception exception) => Exception = exception;
        public FaultResult(string message) => Message = message;
        public Exception Exception { get; } = default!;
        public string Message { get; } = default!;

        protected override void Execute(ActivityExecutionContext activityExecutionContext)
        {
            if(Exception != null!)
                activityExecutionContext.WorkflowExecutionContext.Fault(Exception, activityExecutionContext.ActivityBlueprint.Id, activityExecutionContext.Input, activityExecutionContext.Resuming);
            else 
                activityExecutionContext.WorkflowExecutionContext.Fault(Message!, activityExecutionContext.ActivityBlueprint.Id, activityExecutionContext.Input, activityExecutionContext.Resuming); 
        }
    }
}