using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.Containers;
using Elsa.Activities.Flowcharts;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public class FlowchartConfigurator : ActivityConfigurator<Flowchart>, IFlowchartConfigurator
    {
        public FlowchartConfigurator(IActivityResolver activityResolver) : base(activityResolver)
        {
        }

        public IReadOnlyList<IActivity> Activities => Activity.Activities.ToList().AsReadOnly();

        public NodeConfigurator<T> Add<T>(T activity) where T : class, IActivity
        {
            var node = new NodeConfigurator<T>(this, activity);
            Activity.Activities.Add(node.Activity);
            return node;
        }
        
        public NodeConfigurator<T> Add<T>(Action<T>? setupActivity = default, string? name = default) where T : class, IActivity
        {
            var activity = ActivityResolver.ResolveActivity(setupActivity);
            return Add(activity);
        }

        public FlowchartConfigurator Connect(
            IActivity source,
            IActivity target,
            string? outcome = default)
        {
            Activity.Connections.Add(new Connection(source, target, outcome));
            return this;
        }

        public NodeConfigurator<T> StartWith<T>(Action<T> setupActivity) where T : class, IActivity => Add(setupActivity);
        public NodeConfigurator<T> StartWith<T>(T activity) where T : class, IActivity => Add(activity);

        public override Flowchart Build()
        {
            // Generate deterministic activity ids.
            var id = 1;
            foreach (var activity in Activity.Activities)
            {
                if (string.IsNullOrEmpty(activity.Id))
                    activity.Id = $"activity-{id++}";
            }
            
            return base.Build();
        }
    }
}