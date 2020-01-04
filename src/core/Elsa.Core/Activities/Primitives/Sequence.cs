using System.Collections.Generic;
using System.Linq;
using Elsa.Attributes;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Containers
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

        protected override IActivityExecutionResult OnExecute(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext)
        {
            return Schedule(Activities.Reverse(), activityExecutionContext.Input);
        }
    }
}