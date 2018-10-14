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

        public IfElse(Func<WorkflowExecutionContext, ActivityExecutionContext, bool> condition, IActivity trueBranch, IActivity falseBranch)
        {
            this.condition = condition;
            TrueBranch = trueBranch;
            FalseBranch = falseBranch;
        }
        
        public IActivity TrueBranch { get; set; }
        public IActivity FalseBranch { get; set;}

        protected override ActivityExecutionResult Execute(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext)
        {
            var result = condition(workflowContext, activityContext);
            if (TrueBranch != null && result)
                return ScheduleActivity(TrueBranch);
            
            if(FalseBranch != null && !result)
                return ScheduleActivity(FalseBranch);

            return Noop();
        }
    }
}