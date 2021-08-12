using System.Threading;
using System.Threading.Tasks;

namespace Elsa.WorkflowSettings.Abstractions.Providers
{
    public abstract class WorkflowSettingsProvider : IWorkflowSettingsProvider
    {
        public virtual async ValueTask<object> GetWorkflowSettingAsync(string workflowBlueprintId, string key, CancellationToken cancellationToken)
        {
            return await OnGetWorkflowSettingAsync(workflowBlueprintId, key, cancellationToken);
        }

        protected virtual ValueTask<object> OnGetWorkflowSettingAsync(string workflowBlueprintId, string key, CancellationToken cancellationToken) => new(new object());
    }
}