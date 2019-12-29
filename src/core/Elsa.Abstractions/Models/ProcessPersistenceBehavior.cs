namespace Elsa.Models
{
    public enum ProcessPersistenceBehavior
    {
        /// <summary>
        /// Workflow instances are persisted only when being suspended. 
        /// </summary>
        Suspended,
        
        /// <summary>
        /// Workflow instances are persisted after the workflow executed scheduled activities.
        /// </summary>
        WorkflowExecuted,
        
        /// <summary>
        /// Workflow instances are persisted after each activity that executed.
        /// </summary>
        ActivityExecuted
    }
}