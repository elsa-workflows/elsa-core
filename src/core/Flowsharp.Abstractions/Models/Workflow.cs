using System.Collections.Generic;
using System.Linq;
using Flowsharp.Activities;

namespace Flowsharp.Models
{
    public class Workflow
    {
        public Workflow(IEnumerable<IActivity> activities, IEnumerable<Connection> connections) : this()
        {
            Activities = activities.ToList();
            Connections = connections.ToList();
        }
        
        public Workflow()
        {
            CurrentScope = new WorkflowExecutionScope();
            Scopes = new Stack<WorkflowExecutionScope>(new[]{ CurrentScope });
            Arguments = new Variables();
            BlockingActivities = new List<IActivity>();
            Metadata = new WorkflowMetadata();
        }
        
        public WorkflowStatus Status { get; set; }
        public IList<IActivity> Activities { get; set; } = new List<IActivity>();
        public IList<Connection> Connections { get; set; } = new List<Connection>();
        public Stack<WorkflowExecutionScope> Scopes { get; set; }
        public WorkflowExecutionScope CurrentScope { get; set; }
        public Variables Arguments { get; set; }
        public IList<IActivity> BlockingActivities { get; set; }
        public WorkflowMetadata Metadata { get; set; } 
    }
}