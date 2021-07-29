
namespace Elsa.Services.Models
{
    public class WorkflowSettingsContext
    {
        /// <summary>
        /// Get workflow settings value filtered by Workflow Blueprint Id and Key
        /// </summary>
        /// <param name="workflowBlueprintId">Workflow Blueprint Id</param>
        /// <param name="key">Key</param>
        public WorkflowSettingsContext(string workflowBlueprintId, string key = "disabled")
        {
            WorkflowBlueprintId = workflowBlueprintId;
            Key = key;
        }

        public string WorkflowBlueprintId { get; }
        public string Key { get; }
        public bool Value { get; set; }
    }
}
