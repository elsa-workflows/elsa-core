using System;
using System.Linq;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public class OutcomeBuilder : IOutcomeBuilder
    {
        public OutcomeBuilder(IFlowchartBuilder flowchartBuilder, IActivityBuilder source, string outcome)
        {
            FlowchartBuilder = flowchartBuilder;
            Source = source;
            Outcome = outcome;
        }

        public IFlowchartBuilder FlowchartBuilder { get; }
        public IActivityBuilder Source { get; }
        public string Outcome { get; }

        public IActivityBuilder Then<T>(Action<T>? setup = default, Action<IActivityBuilder>? branch = default, string? name = default) where T : class, IActivity
        {
            var target = FlowchartBuilder.Add(setup, name);
            branch?.Invoke(target);

            FlowchartBuilder.Connect(Source, target, Outcome);
            return target;
        }

        public Activities.Containers.Flowchart Build() => FlowchartBuilder.Build();

        public IConnectionBuilder Then(string activityName)
        {
            return FlowchartBuilder.Connect(
                () => Source, 
                () => FlowchartBuilder.Activities.First(x => x.Activity.Name == activityName), 
                Outcome);
        }
    }
}