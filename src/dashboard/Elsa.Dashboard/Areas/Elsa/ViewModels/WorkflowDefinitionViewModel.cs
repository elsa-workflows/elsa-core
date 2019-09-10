using Elsa.Models;

namespace Elsa.Dashboard.Areas.Elsa.ViewModels
{
    public class WorkflowDefinitionViewModel
    {
        public WorkflowDefinition WorkflowDefinition { get; set; }
        public string Json { get; set; }
        public string SubmitAction { get; set; }
    }
}