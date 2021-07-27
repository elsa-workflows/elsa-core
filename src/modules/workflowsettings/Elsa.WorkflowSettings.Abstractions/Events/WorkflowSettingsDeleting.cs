using Elsa.WorkflowSettings.Models;

namespace Elsa.WorkflowSettings.Abstractions.Events
{
    public class WorkflowSettingsDeleting : WorkflowSettingsNotification
    {
        public WorkflowSettingsDeleting(WorkflowSetting workflowSetting) : base(workflowSetting)
        {
        }
    }
}