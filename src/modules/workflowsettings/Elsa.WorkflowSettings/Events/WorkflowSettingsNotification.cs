using Elsa.WorkflowSettings.Models;
using MediatR;

namespace Elsa.WorkflowSettings.Events
{
    public abstract class WorkflowSettingsNotification : INotification
    {
        public WorkflowSettingsNotification(WorkflowSetting workflowSetting) => WorkflowSetting = workflowSetting;
        public WorkflowSetting WorkflowSetting { get; }
    }
}