using System.Threading;
using System.Threading.Tasks;
using Elsa.WorkflowSettings.Models;

namespace Elsa.WorkflowSettings.Providers
{
    public abstract class WorkflowSettingsProvider : IWorkflowSettingsProvider
    {
        public virtual async ValueTask<WorkflowSetting> GetWorkflowSettingAsync(string workflowBlueprintId, string key, CancellationToken cancellationToken)
        {
            return await OnGetWorkflowSettingAsync(workflowBlueprintId, key, cancellationToken);
        }

        protected virtual ValueTask<WorkflowSetting> OnGetWorkflowSettingAsync(string workflowBlueprintId, string key, CancellationToken cancellationToken) => new(new WorkflowSetting());
    }
}