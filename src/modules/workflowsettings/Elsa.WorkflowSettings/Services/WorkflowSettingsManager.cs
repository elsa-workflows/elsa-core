using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.WorkflowSettings.Models;
using Elsa.WorkflowSettings.Providers;

namespace Elsa.WorkflowSettings.Services
{
    public class WorkflowSettingsManager : IWorkflowSettingsManager
    {
        private readonly IEnumerable<IWorkflowSettingsProvider> _workflowSettingsProviders;

        public WorkflowSettingsManager(IEnumerable<IWorkflowSettingsProvider> workflowSettingsProviders)
        {
            _workflowSettingsProviders = workflowSettingsProviders;
        }

        public async Task<WorkflowSetting> LoadSettingAsync(string workflowBlueprintId, string key, CancellationToken cancellationToken = default)
        {
            var providers = _workflowSettingsProviders;

            var value = new WorkflowSetting();

            foreach (var provider in providers)
            {
                var providerValue = await provider.GetWorkflowSettingAsync(workflowBlueprintId, key, cancellationToken);
                if (providerValue.Value != null)
                {
                    value = providerValue;
                }
            }

            return value;
        }
    }
}
