using Elsa.Models;

namespace Elsa.Dashboard.Areas.Elsa.ViewModels
{
    public class WorkflowInstanceDetailsModel
    {
        public string ReturnUrl { get; set; }
        public string Json { get; set; }
        public WorkflowDefinitionVersion WorkflowDefinition { get; set; }
        public WorkflowInstance WorkflowInstance { get; set; }
    }
}