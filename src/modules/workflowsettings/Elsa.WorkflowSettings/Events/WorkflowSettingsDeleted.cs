using Elsa.WorkflowSettings.Models;

namespace Elsa.WorkflowSettings.Events
{
    public class WorkflowSettingsDeleted : WorkflowSettingsNotification
    {
        public WorkflowSettingsDeleted(WorkflowSetting workflowSetting) : base(workflowSetting)
        {
        }
    }
}