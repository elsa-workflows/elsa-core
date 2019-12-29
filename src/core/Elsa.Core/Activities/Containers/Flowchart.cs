using System.Collections.Generic;
using Elsa.Activities.Workflows.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Containers
{
    public class Flowchart : Activity
    {
        public ICollection<IActivity> Activities
        {
            get => GetState<ICollection<IActivity>>();
            set => SetState(value);
        }
        public ICollection<Connection> Connections { get; set; }
    }
}