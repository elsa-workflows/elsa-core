namespace Elsa.Client.Models
{
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
        /// Workflow instances are persisted after the workflow executed scheduled activities.
        /// </summary>
        WorkflowPassCompleted,

        /// <summary>
        /// Workflow instances are persisted after each activity that executed.
        /// </summary>
        ActivityExecuted
    }
}