using System;
using System.Collections.Generic;
using Elsa.Builders;
using Elsa.Extensions;
using Elsa.Models;

namespace Elsa.Core.Builders
{
    public class WorkflowBuilder
    {
        public WorkflowBuilder(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }
        private readonly ICollection<BuilderNode> nodes = new List<BuilderNode>();
        internal IEnumerable<BuilderNode> Nodes => nodes;

        public ActivityBuilder Add<T>(Action<T> configureActivity = null) where T : IActivity
        {
            var activity = ServiceProvider.CreateActivity<T>();
            configureActivity?.Invoke(activity);
            return Add((IActivity)activity);
        }

        public ActivityBuilder Add(IActivity activity)
        {
            return Add(new ActivityBuilder(this, activity));
        }

        public Workflow BuildWorkflow()
        {
            var workflow = new Workflow();

            foreach (var node in nodes)
            {
                node.ApplyTo(workflow);
            }

            return workflow;
        }

        internal T Add<T>(T node) where T : BuilderNode
        {
            nodes.Add(node);
            return node;
        }
    }
}