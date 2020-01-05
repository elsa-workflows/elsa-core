using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.Flowcharts;
using Elsa.Services;

namespace Elsa.Builders
{
    public class FlowchartBuilder : IActivityBuilder
    {
        private readonly IActivityResolver activityResolver;
        
        public FlowchartBuilder(IActivityResolver activityResolver, IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            this.activityResolver = activityResolver;
            Flowchart = activityResolver.ResolveActivity<Flowchart>();
            Connections = new List<ConnectionBuilder>();
        }
        
        public IServiceProvider ServiceProvider { get; }
        public Flowchart Flowchart { get; }
        public ICollection<ConnectionBuilder> Connections { get; }

        public ConnectionBuilder StartWith<T>(Action<T>? setup = default) where T : class, IActivity
        {
            var activity = activityResolver.ResolveActivity(setup);
            return StartWith(activity);
        }
        
        public ConnectionBuilder StartWith<T>(T activity) where T : class, IActivity
        {
            return new ConnectionBuilder(this, activityResolver, ServiceProvider, activity);
        }
        
        public FlowchartBuilder Add<T>(Action<T>? setup = default) where T : class, IActivity
        {
            var activity = activityResolver.ResolveActivity(setup);
            Flowchart.Activities.Add(activity);
            return this;
        }
        
        public FlowchartBuilder Add<T>(T activity) where T : class, IActivity
        {
            Flowchart.Activities.Add(activity);
            return this;
        }

        public IActivity Build()
        {
            var connections = Connections.Where(x => x.Connection.Target != null).Select(x => x.Connection).ToList();

            Flowchart.Connections = connections;
            return Flowchart;
        }
    }
}