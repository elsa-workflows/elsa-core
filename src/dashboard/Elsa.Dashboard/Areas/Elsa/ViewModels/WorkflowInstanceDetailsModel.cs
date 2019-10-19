using Elsa.Models;
using Elsa.WorkflowDesigner.Models;

namespace Elsa.Dashboard.Areas.Elsa.ViewModels
{
    public class WorkflowInstanceDetailsModel
    {
        public string ReturnUrl { get; set; }
        public WorkflowDefinitionVersion WorkflowDefinition { get; set; }
        public WorkflowModel WorkflowModel { get; set; }
        public ActivityDefinitionModel[] ActivityDefinitions { get; set; }
    }
}