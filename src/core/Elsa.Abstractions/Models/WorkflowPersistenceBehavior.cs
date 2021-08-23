using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Elsa.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum WorkflowPersistenceBehavior
    {
        /// <summary>
        /// Workflow instances are persisted only when being suspended. 
        /// </summary>
        Suspended,
        
        /// <summary>
        /// Workflow instances are persisted after the workflow completed a burst of execution.
        /// </summary>
        WorkflowBurst,
        
        /// <summary>
        /// Workflow instances are persisted after each activity that executed.
        /// </summary>
        ActivityExecuted
    }
}