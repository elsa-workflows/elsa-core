using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.WorkflowSettings.Abstractions.Services.WorkflowSettings;
using Elsa.WorkflowSettings.Models;
using MediatR;

namespace Elsa.WorkflowSettings.Handlers
{
    public class LoadWorkflowSettings : INotificationHandler<WorkflowBlueprintLoaded>
    {
        private readonly IWorkflowSettingsManager _workflowSettingsManager;

        public LoadWorkflowSettings(IWorkflowSettingsManager workflowSettingsManager)
        {
            _workflowSettingsManager = workflowSettingsManager;
        }

        public async Task Handle(WorkflowBlueprintLoaded notification, CancellationToken cancellationToken)
        {
            var workflowSetting = new WorkflowSetting
            {
                WorkflowBlueprintId = notification.WorkflowBlueprint.Id,
                Key = notification.WorkflowBlueprint.Name ?? "disabled"
            };
            notification.WorkflowBlueprint.Value = await LoadWorkflowSettingsAsync(workflowSetting, cancellationToken);
        }

        private async ValueTask<string?> LoadWorkflowSettingsAsync(WorkflowSetting workflowSetting, CancellationToken cancellationToken)
        {            
            return await _workflowSettingsManager.LoadSettingAsync(workflowSetting, cancellationToken);
        }
    }
}
