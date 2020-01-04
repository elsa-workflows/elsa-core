using System;
using System.Linq;
using Elsa.Activities.Flowcharts;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public class OutcomeBuilder
    {
        public OutcomeBuilder(FlowchartConfigurator flowchartConfigurator, IActivity source, string outcome)
        {
            FlowchartConfigurator = flowchartConfigurator;
            Source = source;
            Outcome = outcome;
        }

        public FlowchartConfigurator FlowchartConfigurator { get; }
        public IActivity Source { get; }
        public string Outcome { get; }

        public NodeConfigurator<T> Then<T>(Action<T>? setup = default, Action<NodeConfigurator<T>>? branch = default, string? name = default) where T : class, IActivity
        {
            var target = FlowchartConfigurator.Add(setup, name);
            branch?.Invoke(target);

            FlowchartConfigurator.Connect(Source, target.Activity, Outcome);
            return target;
        }

        public Flowchart Build() => FlowchartConfigurator.Build();
    }
}