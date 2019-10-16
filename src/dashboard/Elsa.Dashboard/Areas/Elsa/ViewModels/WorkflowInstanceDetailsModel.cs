using Elsa.Models;
using Elsa.WorkflowDesigner.Models;

namespace Elsa.Dashboard.Areas.Elsa.ViewModels
{
    public class WorkflowInstanceDetailsModel
    {
        public string ReturnUrl { get; set; }
        public WorkflowDefinitionVersion WorkflowDefinition { get; set; }
        public DesignerWorkflow Workflow { get; set; }
        public DesignerActivityDefinition[] ActivityDefinitions { get; set; }
    }
}