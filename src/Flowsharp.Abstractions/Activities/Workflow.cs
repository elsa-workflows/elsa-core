using System.Collections.Generic;
using System.Linq;
using Flowsharp.ActivityResults;
using Flowsharp.Models;

namespace Flowsharp.Activities
{
    public class Workflow
    {
        public Workflow()
        {
        }

        public Workflow(IEnumerable<IActivity> activities, IEnumerable<Connection> connections)
        {
            Activities = activities.ToList();
            Connections = connections.ToList();    
        }
        
        public IList<IActivity> Activities { get; set; } = new List<IActivity>();
        public IList<Connection> Connections { get; set; } = new List<Connection>();
    }
}