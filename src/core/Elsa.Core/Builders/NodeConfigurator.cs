using System;
using System.Linq;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public class NodeConfigurator<T> : ActivityConfigurator<T> where T : class, IActivity
    {
        public NodeConfigurator(FlowchartConfigurator flowchartConfigurator, T activity) : base(activity)
        {
            FlowchartConfigurator = flowchartConfigurator;
        }

        public NodeConfigurator(FlowchartConfigurator flowchartConfigurator, IActivityResolver activityResolver) : base(activityResolver)
        {
            FlowchartConfigurator = flowchartConfigurator;
        }

        public FlowchartConfigurator FlowchartConfigurator { get; }

        public OutcomeBuilder When(string outcome)
        {
            return new OutcomeBuilder(FlowchartConfigurator, Activity, outcome);
        }

        public NodeConfigurator<TTarget> Then<TTarget>(Action<TTarget>? setup = null, Action<NodeConfigurator<TTarget>>? branch = null, string? name = default) where TTarget: class, IActivity
            => When(null).Then(setup, branch, name);
        
        public NodeConfigurator<TTarget> Then<TTarget>(TTarget target) where TTarget : class, IActivity
        {
            FlowchartConfigurator.Connect(Activity, target);
            return new NodeConfigurator<TTarget>(FlowchartConfigurator, target);
        }
    }
}