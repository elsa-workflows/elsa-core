using Elsa.WorkflowSettings.Models;

namespace Elsa.WorkflowSettings.Events
{
    public class WorkflowSettingsSaving : WorkflowSettingsNotification
    {
        public WorkflowSettingsSaving(WorkflowSetting workflowSetting) : base(workflowSetting)
        {
        }
    }
}