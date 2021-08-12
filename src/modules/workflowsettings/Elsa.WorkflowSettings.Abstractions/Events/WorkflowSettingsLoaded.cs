using Elsa.WorkflowSettings.Models;

namespace Elsa.WorkflowSettings.Abstractions.Events
{
    /// <summary>
    /// Published when a workflow settings loaded.
    /// </summary>
    public class WorkflowSettingsLoaded : WorkflowSettingsNotification
    {
        public WorkflowSettingsLoaded(WorkflowSetting workflowSetting) : base(workflowSetting)
        {
        }
    }
}