using Elsa.Services.Models;

namespace Elsa.Events
{
    /// <summary>
    /// Published when a workflow settings loaded.
    /// </summary>
    public class WorkflowSettingsLoaded : WorkflowSettingsNotification
    {
        public WorkflowSettingsLoaded(WorkflowSettingsContext workflowSettingsContext) : base(workflowSettingsContext)
        {
        }
    }
}