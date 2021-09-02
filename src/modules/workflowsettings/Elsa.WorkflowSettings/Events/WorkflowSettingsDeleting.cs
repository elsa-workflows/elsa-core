using Elsa.WorkflowSettings.Models;

namespace Elsa.WorkflowSettings.Events
{
    public class WorkflowSettingsDeleting : WorkflowSettingsNotification
    {
        public WorkflowSettingsDeleting(WorkflowSetting workflowSetting) : base(workflowSetting)
        {
        }
    }
}