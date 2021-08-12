using System.Threading;
using System.Threading.Tasks;
using Elsa.WorkflowSettings.Abstractions.Persistence;
using Elsa.WorkflowSettings.Abstractions.Providers;
using Elsa.WorkflowSettings.Extensions;

namespace Elsa.WorkflowSettings.Providers
{
    public class DatabaseWorkflowSettingsProvider : WorkflowSettingsProvider
    {
        private readonly IWorkflowSettingsStore _workflowSettingsStore;

        public DatabaseWorkflowSettingsProvider(IWorkflowSettingsStore workflowSettingsStore)
        {
            _workflowSettingsStore = workflowSettingsStore;
        }

        public override async ValueTask<object> GetWorkflowSettingAsync(string workflowBlueprintId, string key, CancellationToken cancellationToken)
        {
            var workflowSettings = await _workflowSettingsStore.FindByWorkflowBlueprintIdAndKeyAsync(workflowBlueprintId, key, cancellationToken);
            
            if (workflowSettings != null && workflowSettings.Value != null)
            {
                return new ValueTask<object>(workflowSettings.Value);
            }

            return new ValueTask<object>(null);
        }
    }
}