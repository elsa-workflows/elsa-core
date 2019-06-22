using System;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Core.Services.WorkflowBuilders
{
    public class ActivityBuilder : IActivityBuilder
    {
        public ActivityBuilder(WorkflowBuilder workflowBuilder, IActivity activity)
        {
            WorkflowBuilder = workflowBuilder;
            Activity = activity;
        }

        public WorkflowBuilder WorkflowBuilder { get; }
        public IActivity Activity { get; }

        public IOutcomeBuilder When(string outcome)
        {
            return new OutcomeBuilder(WorkflowBuilder, this, outcome);
        }

        public IActivityBuilder Then<T>(Action<T> setup = null) where T : IActivity
        {
            return When(null).Then(setup);
        }

        public Workflow Build() => WorkflowBuilder.Build();
    }
}