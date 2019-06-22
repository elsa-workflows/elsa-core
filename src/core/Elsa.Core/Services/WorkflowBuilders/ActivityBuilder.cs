using System;
using System.Collections.Generic;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Core.Services.WorkflowBuilders
{
    public class ActivityBuilder : Builder
    {
        public ActivityBuilder(WorkflowBuilder workflowBuilder, IActivity activity) : base(workflowBuilder)
        {
            Activity = activity;
            Connections = new List<ConnectionBuilder>();
        }
        public IActivity Activity { get; }
        public IList<ConnectionBuilder> Connections { get; }

        public Workflow Build()
        {
            return WorkflowBuilder.Build();
        }
    }
    
    public class ActivityBuilder<T> : ActivityBuilder where T:IActivity
    {
        public ActivityBuilder(WorkflowBuilder workflowBuilder, T activity) : base(workflowBuilder, activity)
        {
        }
        
        public ActivityBuilder<TNext> Add<TNext>(Action<TNext> configure) where TNext : IActivity
        {
            return WorkflowBuilder.Add(configure);
        }

        public ActivityBuilder<TNext> Connect<TNext>(Action<TNext> configure, string outcome = null) where TNext : IActivity
        {
            var activity = Add(configure);
            var connection = new ConnectionBuilder(WorkflowBuilder, this, activity, outcome);

            Connections.Add(connection);
            return activity;
        }
    }
}