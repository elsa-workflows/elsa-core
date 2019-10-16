using Elsa.WorkflowDesigner.Models;

namespace Elsa.Dashboard.Areas.Elsa.ViewModels
{
    public class WorkflowDefinitionEditModel
    {
        public string Id { get; set; }
        public string Json { get; set; }
        public string SubmitAction { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsSingleton { get; set; }
        public bool IsDisabled { get; set; }
        public DesignerActivityDefinition[] ActivityDefinitions { get; set; }
        public DesignerWorkflow DesignerWorkflow { get; set; }
    }
}