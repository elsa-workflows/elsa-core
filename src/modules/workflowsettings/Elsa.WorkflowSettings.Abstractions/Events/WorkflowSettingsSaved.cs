using Elsa.WorkflowSettings.Models;

namespace Elsa.WorkflowSettings.Abstractions.Events
{
    public class WorkflowSettingsSaved : WorkflowSettingsNotification
    {
        public WorkflowSettingsSaved(WorkflowSetting workflowSetting) : base(workflowSetting)
        {
        }
    }
}