using Elsa.Models;
using ActivityDefinition = Elsa.WorkflowDesigner.Models.ActivityDefinition;

namespace Elsa.Dashboard.Areas.Elsa.ViewModels
{
    public class WorkflowInstanceDetailsModel
    {
        public string ReturnUrl { get; set; }
        public string Json { get; set; }
        public WorkflowDefinitionVersion WorkflowDefinition { get; set; }
        public WorkflowInstance WorkflowInstance { get; set; }
        public ActivityDefinition[] ActivityDefinitions { get; set; }
    }
}