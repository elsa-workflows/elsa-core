using System.Collections.Generic;
using System.Linq;
using Flowsharp.Persistence.Models;

namespace Flowsharp.Web.ViewComponents.ViewModels
{
    public class WorkflowEditorViewModel
    {
        public WorkflowEditorViewModel(WorkflowDefinition workflowDefinition, IEnumerable<dynamic> activityShapes)
        {
            WorkflowDefinition = workflowDefinition;
            ActivityShapes = activityShapes.ToList();
        }
        
        public WorkflowDefinition WorkflowDefinition { get; }
        public ICollection<dynamic> ActivityShapes { get; }
    }
}