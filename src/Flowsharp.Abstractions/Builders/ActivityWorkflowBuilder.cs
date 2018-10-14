using System;
using Flowsharp.Activities;
using Flowsharp.Models;

namespace Flowsharp.Builders
{
    public class ActivityWorkflowBuilder
    {
        private readonly IActivity activity;
        private readonly WorkflowBuilder builder;

        public ActivityWorkflowBuilder(WorkflowBuilder builder, IActivity activity)
        {
            this.builder = builder;
            this.activity = activity;
        }

        public ActivityWorkflowBuilder Connect(IActivity target, Action<ActivityWorkflowBuilder> activityBuilder = null)
        {
            var connection = new Connection(activity, target);
            builder.Activities.Add(target);
            builder.Connections.Add(connection);
            
            activityBuilder?.Invoke(new ActivityWorkflowBuilder(builder, target));
            return this;
        }
    }
}