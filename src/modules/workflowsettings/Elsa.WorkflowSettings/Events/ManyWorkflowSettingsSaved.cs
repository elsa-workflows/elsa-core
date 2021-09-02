using System.Collections.Generic;
using Elsa.WorkflowSettings.Models;
using MediatR;

namespace Elsa.WorkflowSettings.Events
{
    public class ManyWorkflowSettingsSaved : INotification
    {
        public ManyWorkflowSettingsSaved(IEnumerable<WorkflowSetting> workflowSettings) => WorkflowSettings = workflowSettings;
        public IEnumerable<WorkflowSetting> WorkflowSettings { get; }
    }
}