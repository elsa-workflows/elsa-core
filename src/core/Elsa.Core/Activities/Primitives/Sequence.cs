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

        protected override IActivityExecutionResult OnChildExecuted(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, ActivityExecutionContext childActivityExecutionContext)
        {
            if(workflowExecutionContext.Status != WorkflowStatus.Running)
                return Noop();

            var activities = Activities.ToList();
            var currentIndex = CurrentIndex;
            
            if (currentIndex < activities.Count)
            {
                var currentActivity = activities[currentIndex++];
                CurrentIndex = currentIndex;
                return Schedule(currentActivity, childActivityExecutionContext.Input);
            }
            
            workflowExecutionContext.EndScope();
            return Done(activityExecutionContext.Input);
        }
    }
}