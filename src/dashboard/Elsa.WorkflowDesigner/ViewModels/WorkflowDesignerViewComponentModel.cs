namespace Elsa.WorkflowDesigner.ViewModels
{
    public class WorkflowDesignerViewComponentModel
    {
        public WorkflowDesignerViewComponentModel(string id, string? activityDefinitionsJson, string? workflowJson, bool isReadonly)
        {
            Id = id;
            ActivityDefinitionsJson = activityDefinitionsJson;
            WorkflowJson = workflowJson;
            IsReadonly = isReadonly;
        }

        public string Id { get; }
        public string? ActivityDefinitionsJson { get; set; }
        public string? WorkflowJson { get; set; }
        public bool IsReadonly { get; set; }
    }
}