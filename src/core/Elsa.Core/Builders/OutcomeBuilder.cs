using System;
using System.Linq;
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

        public IActivityBuilder Then<T>(Action<IActivityBuilder>? branch = default)
            where T : class, IActivity => Then(WorkflowBuilder.Add<T>(branch));

        public IConnectionBuilder Then(string activityName)
        {
            return WorkflowBuilder.Connect(
                () => Source, 
                () => WorkflowBuilder.Activities.First(x => x.Name == activityName), 
                Outcome);
        }

        private IActivityBuilder Then(IActivityBuilder activityBuilder, Action<IActivityBuilder>? branch = default)
        {
            branch?.Invoke(activityBuilder);
            WorkflowBuilder.Connect(Source, activityBuilder, Outcome);
            return activityBuilder;
        }

        public IWorkflowBlueprint Build() => WorkflowBuilder.Build();
    }
}