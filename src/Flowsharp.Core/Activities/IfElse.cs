using System;
using Flowsharp.ActivityResults;
using Flowsharp.Models;

namespace Flowsharp.Activities
{
    public class IfElse : Activity
    {
        private readonly Func<WorkflowExecutionContext, ActivityExecutionContext, bool> condition;

        public IfElse()
        {
        }

        public IfElse(Func<WorkflowExecutionContext, ActivityExecutionContext, bool> condition)
        {
            this.condition = condition;
        }
        

        protected override ActivityExecutionResult Execute(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext)
        {
            var result = condition(workflowContext, activityContext);
            
            return ActivateEndpoint(result ? "True" : "False");
        }
    }
}