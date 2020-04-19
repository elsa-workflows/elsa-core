using System;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public class ActivityBuilder : IActivityBuilder
    {
        public ActivityBuilder(IWorkflowBuilder workflowBuilder, IActivity activity)
        {
            WorkflowBuilder = workflowBuilder;
            Activity = activity;
        }

        public IWorkflowBuilder WorkflowBuilder { get; }
        public IActivity Activity { get; }
        public IActivityBuilder Add<T>(Action<T>? setup = default) where T : class, IActivity => WorkflowBuilder.Add(setup);
        public IOutcomeBuilder When(string? outcome) => new OutcomeBuilder(WorkflowBuilder, this, outcome);
        public IActivityBuilder Then<T>(Action<T>? setup = null, Action<IActivityBuilder>? branch = null) where T : class, IActivity => When(null).Then(setup, branch);

        public IActivityBuilder Then<T>(T activity, Action<IActivityBuilder>? branch = null) where T : class, IActivity => When(null).Then(activity, branch);

        public IActivityBuilder Then(IActivityBuilder targetActivity)
        {
            WorkflowBuilder.Connect(this, targetActivity);
            return this;
        }

        public IActivity BuildActivity() => Activity;
        public Workflow Build() => WorkflowBuilder.Build();
    }
}