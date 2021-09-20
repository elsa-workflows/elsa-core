using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.WorkflowSettings.Models;
using Elsa.WorkflowSettings.Services;
using MediatR;

namespace Elsa.WorkflowSettings.Handlers
{
    public class LoadWorkflowSettingDisabledHandler : INotificationHandler<WorkflowBlueprintLoaded>
    {
        private readonly IWorkflowSettingsManager _workflowSettingsManager;

        public LoadWorkflowSettingDisabledHandler(IWorkflowSettingsManager workflowSettingsManager)
        {
            _workflowSettingsManager = workflowSettingsManager;
        }

        public async Task Handle(WorkflowBlueprintLoaded notification, CancellationToken cancellationToken)
        {
            var result = await LoadWorkflowSettingAsync(notification.WorkflowBlueprint.Id, "disabled", cancellationToken);
            notification.WorkflowBlueprint.IsDisabled = Convert.ToBoolean(result.Value ?? "false");
        }

        private async ValueTask<WorkflowSetting> LoadWorkflowSettingAsync(string workflowBlueprintId, string key, CancellationToken cancellationToken)
        {
            return await _workflowSettingsManager.LoadSettingAsync(workflowBlueprintId, key, cancellationToken);
        }
    }
}