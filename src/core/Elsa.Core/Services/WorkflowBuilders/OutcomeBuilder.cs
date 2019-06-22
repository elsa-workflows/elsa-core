using System;
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

        public IActivityBuilder Then<T>(Action<T> setup) where T : IActivity
        {
            var target = WorkflowBuilder.Add(setup);

            WorkflowBuilder.Connect(Source, target, Outcome);
            return target;
        }

        public Workflow Build() => WorkflowBuilder.Build();
    }
}