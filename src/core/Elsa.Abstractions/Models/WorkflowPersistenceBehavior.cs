using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Elsa.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum WorkflowPersistenceBehavior
    {
        // TODO: Consider a new service to "get the default persistence behaviour".
        // Currently it is set to WorkflowBurst implicitly because it is the default
        // for the enum type.  This would be more clear if explicit.
        // This includes altering all locations which need a default persistence
        // behaviour though.

        /// <summary>
        /// Workflow instances are persisted after the workflow completed a burst of execution.
        /// </summary>
        WorkflowBurst = 0,

        /// <summary>
        /// Workflow instances are persisted only when being suspended. 
        /// </summary>
        Suspended,
        
        /// <summary>
        /// Workflow instances are persisted after the workflow executed scheduled activities.
        /// </summary>
        WorkflowPassCompleted,

        /// <summary>
        /// Workflow instances are persisted after each activity that executed.
        /// </summary>
        ActivityExecuted
    }
}