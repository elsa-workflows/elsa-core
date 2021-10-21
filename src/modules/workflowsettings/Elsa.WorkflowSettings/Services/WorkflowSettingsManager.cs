using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Specifications;
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

            foreach (var provider in providers.OrderByDescending(x => x.Priority))
            {
                var providerValue = await provider.GetWorkflowSettingAsync(workflowBlueprintId, key, cancellationToken);
                if (providerValue.Value != null)
                {
                    value = providerValue;
                }
            }

            return value;
        }

        public async Task<IEnumerable<WorkflowSetting>> LoadSettingsAsync(string workflowBlueprintId, CancellationToken cancellationToken = default, IOrderBy<WorkflowSetting>? orderBy = default, IPaging? paging = default)
        {
            var providers = _workflowSettingsProviders;

            var settingsDictionary = new Dictionary<string, WorkflowSetting>();

            foreach (var provider in providers.OrderByDescending(x => x.Priority))
            {
                var settings = await provider.GetWorkflowSettingsAsync(workflowBlueprintId, cancellationToken, orderBy, paging);

                foreach (var setting in settings)
                    settingsDictionary[setting.Key] = setting;
            
            }

            return settingsDictionary.Values;
        }
    }
}
