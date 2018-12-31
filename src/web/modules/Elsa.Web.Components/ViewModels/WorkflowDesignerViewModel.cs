using System.Collections.Generic;
using System.Linq;
using Elsa.Models;

namespace Elsa.Web.Components.ViewModels
{
    public class WorkflowDesignerViewModel
    {
        public WorkflowDesignerViewModel(Workflow workflow, IEnumerable<dynamic> activityShapes)
        {
            Workflow = workflow;
            ActivityShapes = activityShapes.ToList();
            Connections = workflow.Connections.Select(x => new ConnectionModel(x)).ToList();
        }
        
        public Workflow Workflow { get; }
        public IReadOnlyCollection<ConnectionModel> Connections { get; }
        public ICollection<dynamic> ActivityShapes { get; }
    }
}