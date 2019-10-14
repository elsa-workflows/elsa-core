namespace Elsa.WorkflowDesigner.ViewModels
{
    public class WorkflowDesignerViewComponentModel
    {
        public WorkflowDesignerViewComponentModel(string id, string? activityDefinitionsJson, string? workflowJson)
        {
            Id = id;
            ActivityDefinitionsJson = activityDefinitionsJson;
            WorkflowJson = workflowJson;
        }

        public string Id { get; }
        public string? ActivityDefinitionsJson { get; set; }
        public string? WorkflowJson { get; set; }
    }
}