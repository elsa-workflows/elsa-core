using System;
using Elsa.Activities.Containers;
using Elsa.Activities.Flowcharts;
using Elsa.Services;

namespace Elsa.Builders
{
    public class ConnectionBuilder : IActivityBuilder
    {
        private readonly FlowchartBuilder flowchartBuilder;
        private readonly IActivityResolver activityResolver;

        public ConnectionBuilder(FlowchartBuilder flowchartBuilder, IActivityResolver activityResolver, IServiceProvider serviceProvider, IActivity source)
        {
            ServiceProvider = serviceProvider;
            Connection = new Connection
            {
                Source = new SourceEndpoint
                {
                    Activity = source
                }
            };
            
            this.flowchartBuilder = flowchartBuilder;
            this.activityResolver = activityResolver;
            this.flowchartBuilder.Flowchart.Activities.Add(source);
            this.flowchartBuilder.Connections.Add(this);
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
            flowchartBuilder.Add(activity);
            Connection.Target = new TargetEndpoint(activity);
            
            var nextConnection = new ConnectionBuilder(flowchartBuilder, activityResolver, ServiceProvider, activity);
            connection?.Invoke(nextConnection);
            return nextConnection;
        }

        public IActivity Build() => flowchartBuilder.Build();
    }
}