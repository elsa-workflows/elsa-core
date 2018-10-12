using System.Collections.Generic;

namespace Flowsharp.Models
{
    public class WorkflowType
    {
        public string Name { get; set; }
        public ICollection<ActivityType> Activities { get; set; }
        public ICollection<Transition> Transitions { get; set; }
    }
}
