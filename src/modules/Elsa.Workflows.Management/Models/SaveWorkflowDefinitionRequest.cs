using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

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
        public WorkflowDefinitionModel WorkflowDefinitionModel { get; set; } = default!;
        
        /// <summary>
        /// The type of <see cref="IWorkflowActivationStrategy"/> to apply when new instances are requested to be created.
        /// </summary>
        public WorkflowOptions? Options { get; set; }
        
        /// <summary>
        /// Whether the workflow definition should be published.
        /// </summary>
        public bool Publish { get; set; }
    }
}
