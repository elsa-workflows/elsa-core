using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events
{    
    /// <summary>
    /// Common base for workflow-settings events.
    /// </summary>
    public abstract class WorkflowSettingsNotification : INotification
    {
        protected WorkflowSettingsNotification(WorkflowSettingsContext workflowSettingsContext) =>
            WorkflowSettingsContext = workflowSettingsContext;

        public WorkflowSettingsContext WorkflowSettingsContext { get; }
    }
}