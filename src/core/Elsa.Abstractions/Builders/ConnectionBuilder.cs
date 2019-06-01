using System;
using System.Linq;
using Elsa.Extensions;
using Elsa.Models;

namespace Elsa.Builders
{
    public class ConnectionBuilder : BuilderNode
    {
        public ConnectionBuilder(WorkflowBuilder workflowBuilder, IActivity sourceActivity, string sourceEndpointName, IActivity targetActivity) : base(workflowBuilder, sourceActivity)
        {
            WorkflowBuilder = workflowBuilder;
            SourceEndpointName = sourceEndpointName;
            TargetActivity = targetActivity;
        }

        public ConnectionBuilder(WorkflowBuilder workflowBuilder, IActivity sourceActivity, string sourceEndpointName, string targetActivityName) : base(workflowBuilder, sourceActivity)
        {
            WorkflowBuilder = workflowBuilder;
            SourceEndpointName = sourceEndpointName;
            TargetActivityName = targetActivityName;
        }

        public IActivity SourceActivity => Activity;
        public string SourceEndpointName { get; }
        public IActivity TargetActivity { get; }
        public string TargetActivityName { get; }

        public ConnectionBuilder Next(IActivity activity, Action<ConnectionBuilder> builderAction)
        {
            return Next(activity, EndpointNames.Done, builderAction);
        }
        
        public ConnectionBuilder Next(IActivity activity, string sourceEndpointName = EndpointNames.Done, Action<ConnectionBuilder> builderAction = null)
        {
            var connectionBuilder = new ConnectionBuilder(WorkflowBuilder, TargetActivity, sourceEndpointName, activity);
            builderAction?.Invoke(connectionBuilder);
            return WorkflowBuilder.Add(connectionBuilder);
        }

        public ConnectionBuilder Next(string activityName, string sourceEndpointName = EndpointNames.Done)
        {
            return WorkflowBuilder.Add(
                new ConnectionBuilder(WorkflowBuilder, TargetActivity, sourceEndpointName, activityName));
        }
        
        public override void ApplyTo(Workflow workflow)
        {
            var sourceActivity = SourceActivity;
            var targetActivity = ResolveTargetActivity();
            
            workflow.Activities.Add(targetActivity);
            workflow.Connections.Add(sourceActivity, targetActivity, SourceEndpointName);
        }

        private IActivity ResolveTargetActivity()
        {
            return TargetActivity ?? WorkflowBuilder.Nodes.First(x => x.Activity.Alias == TargetActivityName).Activity;
        }
    }
}