using Elsa.WorkflowSettings.Models;

namespace Elsa.WorkflowSettings.Abstractions.Events
{
    public class WorkflowSettingsDeleted : WorkflowSettingsNotification
    {
        public WorkflowSettingsDeleted(WorkflowSetting workflowSetting) : base(workflowSetting)
        {
        }
    }
}