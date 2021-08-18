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
            var workflowSetting = new WorkflowSetting
            {
                WorkflowBlueprintId = notification.WorkflowBlueprint.Id,
                Key = "disabled"
            };

            var result = await LoadWorkflowSettingsAsync(workflowSetting, cancellationToken);
            notification.WorkflowBlueprint.IsDisabled = Convert.ToBoolean(result.Value ?? "false");
        }

        private async ValueTask<WorkflowSetting> LoadWorkflowSettingsAsync(WorkflowSetting workflowSetting, CancellationToken cancellationToken)
        {
            return await _workflowSettingsManager.LoadSettingAsync(workflowSetting, cancellationToken);
        }
    }
}