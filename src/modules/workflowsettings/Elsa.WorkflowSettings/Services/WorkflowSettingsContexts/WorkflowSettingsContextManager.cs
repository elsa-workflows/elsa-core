using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;
using Elsa.WorkflowSettings.Abstractions.Persistence;
using Elsa.WorkflowSettings.Abstractions.Services.WorkflowSettingsContexts;
using Elsa.WorkflowSettings.Extensions;

namespace Elsa.WorkflowSettings.Services.WorkflowSettingsContexts
{
    public class WorkflowSettingsContextManager : IWorkflowSettingsContextManager
    {
        private readonly IWorkflowSettingsStore _workflowSettingsStore;

        public WorkflowSettingsContextManager(IWorkflowSettingsStore workflowSettingsStore)
        {
            _workflowSettingsStore = workflowSettingsStore;
        }

        public async ValueTask<bool> LoadContext(WorkflowSettingsContext context, CancellationToken cancellationToken = default)
        {
            var workflowBlueprintId = context.WorkflowBlueprintId;
            var key = context.Key;

            // Read workflow settings from database store persistence
            var workflowSettings = await _workflowSettingsStore.FindByWorkflowBlueprintIdAndKeyAsync(workflowBlueprintId, key, cancellationToken);
            if (workflowSettings != null) 
            {
                return bool.Parse(workflowSettings.Value);
            }
            
            // Read workflow settings from Environment Variables
            var value = Environment.GetEnvironmentVariable($"{workflowBlueprintId}:{key}");
            return bool.Parse(value ?? "false");
        }
    }
}
