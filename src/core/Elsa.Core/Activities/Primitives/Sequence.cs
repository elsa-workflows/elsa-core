using System.Collections.Generic;
using System.Linq;
using Elsa.Attributes;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Primitives
{
    [ActivityDefinition(
        DisplayName = "Sequence",
        Description = "Run a set of activities in sequence.",
        Category = "Primitives",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class Sequence : Activity
    {
        public Sequence()
        {
            Activities = new List<IActivity>();
        }

        public Sequence(params IActivity[] activities)
        {
            Activities = activities.ToList();
        }

        public ICollection<IActivity> Activities
        {
            get => GetState<ICollection<IActivity>>();
            set => SetState(value);
        }

        public int CurrentIndex
        {
            get => GetState(() => 0);
            set => SetState(value);
        }

        protected override IActivityExecutionResult OnExecute(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext)
        {
            workflowExecutionContext.BeginScope(this);
            return Execute(workflowExecutionContext, activityExecutionContext);
        }

        protected override IActivityExecutionResult OnChildExecuted(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext)
        {
            if(workflowExecutionContext.Status != WorkflowStatus.Running)
                return Noop();

            return Execute(workflowExecutionContext, activityExecutionContext);
        }

        private IActivityExecutionResult Execute(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext)
        {
            var activities = Activities.ToList();
            var currentIndex = CurrentIndex;
            
            if (currentIndex < activities.Count)
            {
                var currentActivity = activities[currentIndex++];
                CurrentIndex = currentIndex;
                return Schedule(currentActivity, activityExecutionContext.Input);
            }
            
            workflowExecutionContext.EndScope();
            return Done(activityExecutionContext.Input);
        }
    }
}