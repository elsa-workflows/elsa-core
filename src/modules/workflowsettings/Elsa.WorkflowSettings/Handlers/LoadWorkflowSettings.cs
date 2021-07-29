using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Services.Models;
using Elsa.WorkflowSettings.Abstractions.Services.WorkflowSettingsContexts;
using MediatR;

namespace Elsa.WorkflowSettings.Handlers
{
    public class LoadWorkflowSettings : INotificationHandler<WorkflowSettingsLoaded>
    {
        private readonly IWorkflowSettingsContextManager _workflowSettingsContextManager;

        public LoadWorkflowSettings(IWorkflowSettingsContextManager workflowSettingsContextManager)
        {
            _workflowSettingsContextManager = workflowSettingsContextManager;
        }

        public async Task Handle(WorkflowSettingsLoaded notification, CancellationToken cancellationToken)
        {
            var workflowSettingsContext = notification.WorkflowSettingsContext;
            workflowSettingsContext.Value = await LoadWorkflowSettingsAsync(workflowSettingsContext, cancellationToken);
        }

        private async ValueTask<bool> LoadWorkflowSettingsAsync(WorkflowSettingsContext workflowSettingsContext, CancellationToken cancellationToken)
        {            
            return await _workflowSettingsContextManager.LoadContext(workflowSettingsContext, cancellationToken);
        }
    }
}
