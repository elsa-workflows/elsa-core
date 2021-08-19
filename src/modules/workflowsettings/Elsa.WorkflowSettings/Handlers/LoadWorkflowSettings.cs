using System;
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
            var result = await LoadWorkflowSettingsAsync(notification.WorkflowBlueprint.Id, notification.WorkflowBlueprint.Key, cancellationToken);

            switch (notification.WorkflowBlueprint.Key)
            {
                case "disabled":
                    notification.WorkflowBlueprint.IsDisabled = Convert.ToBoolean(result.Value ?? "false");
                    break;
                default:
                    throw new NotImplementedException($"The key {notification.WorkflowBlueprint.Key} is not supported");
            }
            
        }

        private async ValueTask<WorkflowSetting> LoadWorkflowSettingsAsync(string workflowBlueprintId, string key, CancellationToken cancellationToken)
        {
            return await _workflowSettingsManager.LoadSettingAsync(workflowBlueprintId, key, cancellationToken);
        }
    }
}