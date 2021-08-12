using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.WorkflowSettings.Abstractions.Persistence;
using Elsa.WorkflowSettings.Abstractions.Services.WorkflowSettings;
using Elsa.WorkflowSettings.Extensions;
using Elsa.WorkflowSettings.Models;

namespace Elsa.WorkflowSettings.Services.WorkflowSettingsContexts
{
    public class WorkflowSettingsManager : IWorkflowSettingsManager
    {
        private readonly IWorkflowSettingsStore _workflowSettingsStore;

        public WorkflowSettingsManager(IWorkflowSettingsStore workflowSettingsStore)
        {
            _workflowSettingsStore = workflowSettingsStore;
        }

        public async ValueTask<string?> LoadSettingAsync(WorkflowSetting workflowSetting, CancellationToken cancellationToken = default)
        {
            var workflowBlueprintId = workflowSetting.WorkflowBlueprintId;
            var key = workflowSetting.Key;

            // Read workflow settings from database store persistence
            var workflowSettings = await _workflowSettingsStore.FindByWorkflowBlueprintIdAndKeyAsync(workflowBlueprintId, key, cancellationToken);
            if (workflowSettings != null) 
            {
                return workflowSettings.Value;
            }
            
            // Read workflow settings from Environment Variables
            var value = Environment.GetEnvironmentVariable($"{workflowBlueprintId}:{key}");
            return value;
        }
    }
}
