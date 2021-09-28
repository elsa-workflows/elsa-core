using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Specifications;
using Elsa.WorkflowSettings.Models;

namespace Elsa.WorkflowSettings.Providers
{
    public abstract class WorkflowSettingsProvider : IWorkflowSettingsProvider
    {
        public virtual async ValueTask<WorkflowSetting> GetWorkflowSettingAsync(string workflowBlueprintId, string key, CancellationToken cancellationToken)
        {
            return await OnGetWorkflowSettingAsync(workflowBlueprintId, key, cancellationToken);
        }

        public virtual async ValueTask<IEnumerable<WorkflowSetting>> GetWorkflowSettingsAsync(string workflowBlueprintId, CancellationToken cancellationToken = default, IOrderBy<WorkflowSetting>? orderBy = default, IPaging? paging = default)
        {
            return await OnGetWorkflowSettingsAsync(workflowBlueprintId, cancellationToken);
        }

        protected virtual ValueTask<WorkflowSetting> OnGetWorkflowSettingAsync(string workflowBlueprintId, string key, CancellationToken cancellationToken) => new(new WorkflowSetting());
        protected virtual ValueTask<IEnumerable<WorkflowSetting>> OnGetWorkflowSettingsAsync(string workflowBlueprintId, CancellationToken cancellationToken) => new(new List<WorkflowSetting>());
    }
}