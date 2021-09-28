using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Specifications;
using Elsa.WorkflowSettings.Extensions;
using Elsa.WorkflowSettings.Models;
using Elsa.WorkflowSettings.Persistence;

namespace Elsa.WorkflowSettings.Providers
{
    public class DatabaseWorkflowSettingsProvider : WorkflowSettingsProvider
    {
        private readonly IWorkflowSettingsStore _workflowSettingsStore;

        public DatabaseWorkflowSettingsProvider(IWorkflowSettingsStore workflowSettingsStore)
        {
            _workflowSettingsStore = workflowSettingsStore;
        }

        public override async ValueTask<WorkflowSetting> GetWorkflowSettingAsync(string workflowBlueprintId, string key, CancellationToken cancellationToken)
        {
            var workflowSettings = await _workflowSettingsStore.FindByWorkflowBlueprintIdAndKeyAsync(workflowBlueprintId, key, cancellationToken);
            
            if (workflowSettings != null && workflowSettings.Value != null)
            {
                return await new ValueTask<WorkflowSetting>(new WorkflowSetting { Value = workflowSettings.Value });
            }

            return await new ValueTask<WorkflowSetting>(new WorkflowSetting());
        }

        public override async ValueTask<IEnumerable<WorkflowSetting>> GetWorkflowSettingsAsync(string workflowBlueprintId, CancellationToken cancellationToken = default, IOrderBy<WorkflowSetting>? orderBy = default, IPaging? paging = default)
        {
            var workflowSettings = await _workflowSettingsStore.FindByWorkflowBlueprintIdAsync(workflowBlueprintId, cancellationToken, orderBy, paging);

            if (workflowSettings != null)
            {
                return await new ValueTask<IEnumerable<WorkflowSetting>>(workflowSettings);
            }

            return await new ValueTask<IEnumerable<WorkflowSetting>>(new List<WorkflowSetting>());
        }
    }
}