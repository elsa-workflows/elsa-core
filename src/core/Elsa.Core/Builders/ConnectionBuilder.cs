using System;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public class ConnectionBuilder : IActivityBuilder
    {
        private readonly IWorkflowBuilder workflowBuilder;
        private readonly IActivityResolver activityResolver;

        public ConnectionBuilder(IWorkflowBuilder workflowBuilder, IActivityResolver activityResolver, IServiceProvider serviceProvider, IActivity source)
        {
            ServiceProvider = serviceProvider;
            Connection = new Connection
            {
                Source = new SourceEndpoint
                {
                    Activity = source
                }
            };
            
            this.workflowBuilder = workflowBuilder;
            this.activityResolver = activityResolver;
            this.workflowBuilder.Add(this);
        }

        public IServiceProvider ServiceProvider { get; }
        public Connection Connection { get; }

        public ConnectionBuilder When(string outcome)
        {
            Connection.Source.Outcome = outcome;
            return this;
        }
        
        public ConnectionBuilder Then<T>(Action<T> setup, Action<ConnectionBuilder>? connection = default) where T : class, IActivity
        {
            var activity = activityResolver.ResolveActivity(setup);
            return Then(activity, connection);
        }

        public ConnectionBuilder Then<T>(Action<ConnectionBuilder>? connection = default) where T : class, IActivity
        {
            var activity = activityResolver.ResolveActivity<T>();
            return Then(activity, connection);
        }
        
        public ConnectionBuilder Then<T>(T activity, Action<ConnectionBuilder>? connection = default) where T : class, IActivity
        {
            workflowBuilder.Add(activity);
            Connection.Target = new TargetEndpoint(activity);
            
            var nextConnection = new ConnectionBuilder(workflowBuilder, activityResolver, ServiceProvider, activity);
            connection?.Invoke(nextConnection);
            return nextConnection;
        }

        public IActivity Build() => workflowBuilder.Build();
    }
}