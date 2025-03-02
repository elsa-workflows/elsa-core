namespace Elsa.Workflows.Management.Models
{
    /// <summary>
    /// Represents a request to save a workflow definition.
    /// </summary>
    public class SaveWorkflowDefinitionRequest
    {
        /// <summary>
        /// The workflow definition to save.
        /// </summary>
        public WorkflowDefinitionModel Model { get; set; } = null!;

        /// <summary>
        /// Whether the workflow definition should be published.
        /// </summary>
        public bool? Publish { get; set; }
    }
}
