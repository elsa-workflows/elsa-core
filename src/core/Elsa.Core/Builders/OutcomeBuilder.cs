using System;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public class OutcomeBuilder : IOutcomeBuilder
    {
        public OutcomeBuilder(IWorkflowBuilder workflowBuilder, IActivityBuilder source, string outcome = "Done")
        {
            WorkflowBuilder = workflowBuilder;
            Source = source;
            Outcome = outcome;
        }

        public IWorkflowBuilder WorkflowBuilder { get; }
        public IActivityBuilder Source { get; }
        public string Outcome { get; }

        public IActivityBuilder Then<T>(
            Action<ISetupActivity<T>>? setup = default,
            Action<IActivityBuilder>? branch = default) where T : class, IActivity =>
            Then(WorkflowBuilder.Add(setup), branch);

        public IActivityBuilder Then<T>(T activity, Action<IActivityBuilder>? branch = default)
            where T : class, IActivity => Then(WorkflowBuilder.Add(activity), branch);

        private IActivityBuilder Then(IActivityBuilder activityBuilder, Action<IActivityBuilder>? branch = default)
        {
            branch?.Invoke(activityBuilder);
            WorkflowBuilder.Connect(Source, activityBuilder, Outcome);
            return activityBuilder;
        }

        public Workflow Build() => WorkflowBuilder.Build();
    }
}