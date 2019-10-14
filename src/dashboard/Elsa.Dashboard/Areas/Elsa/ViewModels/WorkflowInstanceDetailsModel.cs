using Elsa.Models;
using Elsa.Services.Models;
using ActivityDefinition = Elsa.WorkflowDesigner.Models.ActivityDefinition;

namespace Elsa.Dashboard.Areas.Elsa.ViewModels
{
    public class WorkflowInstanceDetailsModel
    {
        public string ReturnUrl { get; set; }
        public WorkflowDefinitionVersion WorkflowDefinition { get; set; }
        public Workflow Workflow { get; set; }
        public ActivityDefinition[] ActivityDefinitions { get; set; }
    }
}