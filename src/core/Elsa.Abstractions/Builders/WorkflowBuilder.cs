using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Builders
{
    public class WorkflowBuilder
    {
        private readonly ICollection<BuilderNode> nodes = new List<BuilderNode>();
        internal IEnumerable<BuilderNode> Nodes => nodes;

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