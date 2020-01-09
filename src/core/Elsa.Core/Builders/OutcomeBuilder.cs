using System;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public class OutcomeBuilder
    {
        public OutcomeBuilder(IWorkflowBuilder workflowBuilder, ActivityBuilder source, string outcome)
        {
            WorkflowBuilder = workflowBuilder;
            Source = source;
            Outcome = outcome;
        }

        public IWorkflowBuilder WorkflowBuilder { get; }
        public ActivityBuilder Source { get; }
        public string Outcome { get; }

        public ActivityBuilder Then<T>(Action<T> setup = default, Action<ActivityBuilder> branch = default) where T : class, IActivity
        {
            return Then(WorkflowBuilder.Add(setup), branch);
        }
        
        public ActivityBuilder Then<T>(T activity, Action<ActivityBuilder> branch = default) where T : class, IActivity
        {
            return Then(WorkflowBuilder.Add(activity), branch);
        }
        
        private ActivityBuilder Then(ActivityBuilder activityBuilder, Action<ActivityBuilder> branch = default)
        {
            branch?.Invoke(activityBuilder);
            WorkflowBuilder.Connect(Source, activityBuilder, Outcome);
            return activityBuilder;
        }

        public Workflow Build() => WorkflowBuilder.Build();
    }
}