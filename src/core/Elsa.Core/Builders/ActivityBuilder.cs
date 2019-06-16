using System;
using Elsa.Core.Builders;
using Elsa.Extensions;
using Elsa.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Builders
{
    public class ActivityBuilder : BuilderNode
    {
        public ActivityBuilder(WorkflowBuilder workflowBuilder, IActivity activity) : base(workflowBuilder, activity)
        {
            WorkflowBuilder = workflowBuilder;
        }

        private IServiceProvider ServiceProvider => WorkflowBuilder.ServiceProvider;

        public ConnectionBuilder Next<T>(
            Action<T> configureActivity = null, 
            string sourceEndpointName = EndpointNames.Done, 
            Action<ConnectionBuilder> configureConnection = null)
            where T : IActivity
        {
            var activity = ServiceProvider.CreateActivity<T>();
            configureActivity?.Invoke(activity);
            return Next(activity);
        }

        public ConnectionBuilder Next(IActivity activity, Action<ConnectionBuilder> configureConnection)
        {
            return Next(activity, EndpointNames.Done, configureConnection);
        }

        public ConnectionBuilder Next(IActivity activity, string sourceEndpointName = EndpointNames.Done, Action<ConnectionBuilder> configureconnection = null)
        {
            var connectionBuilder = new ConnectionBuilder(WorkflowBuilder, Activity, sourceEndpointName, activity);
            configureconnection?.Invoke(connectionBuilder);
            return WorkflowBuilder.Add(connectionBuilder);
        }

        public ConnectionBuilder Next(string activityName, string sourceEndpointName = EndpointNames.Done)
        {
            return WorkflowBuilder.Add(
                new ConnectionBuilder(WorkflowBuilder, Activity, sourceEndpointName, activityName)
            );
        }

        public override void ApplyTo(Workflow workflow)
        {
            workflow.Activities.Add(Activity);
        }
    }
}