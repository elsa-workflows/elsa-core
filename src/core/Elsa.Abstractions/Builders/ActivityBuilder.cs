using System;
using Elsa.Models;

namespace Elsa.Builders
{
    public class ActivityBuilder : BuilderNode
    {
        public ActivityBuilder(WorkflowBuilder workflowBuilder, IActivity activity) : base(workflowBuilder, activity)
        {
            WorkflowBuilder = workflowBuilder;
        }


        public ConnectionBuilder Next(IActivity activity, Action<ConnectionBuilder> builderAction)
        {
            return Next(activity, EndpointNames.Done, builderAction);
        }
        
        public ConnectionBuilder Next(IActivity activity, string sourceEndpointName = EndpointNames.Done, Action<ConnectionBuilder> builderAction = null)
        {
            var connectionBuilder = new ConnectionBuilder(WorkflowBuilder, Activity, sourceEndpointName, activity);
            builderAction?.Invoke(connectionBuilder);
            return WorkflowBuilder.Add(connectionBuilder);
        }

        public ConnectionBuilder Next(string activityName, string sourceEndpointName = EndpointNames.Done)
        {
            return WorkflowBuilder.Add(
                new ConnectionBuilder(WorkflowBuilder, Activity, sourceEndpointName, activityName));
        }

        public override void ApplyTo(Workflow workflow)
        {
            workflow.Activities.Add(Activity);
        }
    }
}