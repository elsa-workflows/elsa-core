using System;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public class ActivityBuilder : IBuilder
    {
        public ActivityBuilder(IWorkflowBuilder workflowBuilder, IActivity activity)
        {
            WorkflowBuilder = workflowBuilder;
            Activity = activity;
        }

        public IWorkflowBuilder WorkflowBuilder { get; }
        public IActivity Activity { get; }

        public ActivityBuilder Add<T>(Action<T> setup = default) where T : class, IActivity
        {
            return WorkflowBuilder.Add(setup);
        }

        public OutcomeBuilder When(string outcome)
        {
            return new OutcomeBuilder(WorkflowBuilder, this, outcome);
        }

        public ActivityBuilder Then<T>(Action<T> setup = null, Action<ActivityBuilder> branch = null) where T : class, IActivity
        {
            return When(null).Then(setup, branch);
        }
        
        public ActivityBuilder Then<T>(T activity, Action<ActivityBuilder> branch = null) where T : class, IActivity
        {
            return When(null).Then(activity, branch);
        }

        public ActivityBuilder Then(ActivityBuilder targetActivity)
        {
            WorkflowBuilder.Connect(this, targetActivity);
            return this;
        }

        public IActivity BuildActivity() => Activity;
        public Workflow Build() => WorkflowBuilder.Build();
    }
}