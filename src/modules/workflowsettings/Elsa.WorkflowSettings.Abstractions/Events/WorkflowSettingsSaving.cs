using Elsa.WorkflowSettings.Models;

namespace Elsa.WorkflowSettings.Abstractions.Events
{
    public class WorkflowSettingsSaving : WorkflowSettingsNotification
    {
        public WorkflowSettingsSaving(WorkflowSetting workflowSetting) : base(workflowSetting)
        {
        }
    }
}