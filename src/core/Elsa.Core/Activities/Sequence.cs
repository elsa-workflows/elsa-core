using System.Collections.Generic;
using System.Linq;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities
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

        public ICollection<IActivity> Activities
        {
            get => GetState<ICollection<IActivity>>();
            set => SetState(value);
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            return Combine(Done(), ScheduleActivities(Activities.Reverse(), context.Input));
        }
    }
}