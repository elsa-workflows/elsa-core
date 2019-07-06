using System;
using System.Linq;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Core.Services.WorkflowBuilders
{
    public class OutcomeBuilder : IOutcomeBuilder
    {
        public OutcomeBuilder(IWorkflowBuilder workflowBuilder, IActivityBuilder source, string outcome)
        {
            WorkflowBuilder = workflowBuilder;
            Source = source;
            Outcome = outcome;
        }

        public IWorkflowBuilder WorkflowBuilder { get; }
        public IActivityBuilder Source { get; }
        public string Outcome { get; }

        public IActivityBuilder Then<T>(Action<T> setup = default, string id = default) where T : class, IActivity
        {
            var target = WorkflowBuilder.Add(setup, id);

            WorkflowBuilder.Connect(Source, target, Outcome);
            return target;
        }

        public WorkflowDefinition Build() => WorkflowBuilder.Build();

        public IConnectionBuilder Then(string activityId)
        {
            return WorkflowBuilder.Connect(
                () => Source, 
                () => WorkflowBuilder.Activities.First(x => x.Id == activityId), 
                Outcome);
        }
    }
}