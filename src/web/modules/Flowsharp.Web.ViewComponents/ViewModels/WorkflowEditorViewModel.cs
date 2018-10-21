using System.Collections.Generic;
using System.Linq;
using Flowsharp.Models;

namespace Flowsharp.Web.ViewComponents.ViewModels
{
    public class WorkflowEditorViewModel
    {
        public WorkflowEditorViewModel(Workflow workflow, IEnumerable<dynamic> activityShapes)
        {
            Workflow = workflow;
            ActivityShapes = activityShapes.ToList();
        }
        
        public Workflow Workflow { get; }
        public ICollection<dynamic> ActivityShapes { get; }
    }
}