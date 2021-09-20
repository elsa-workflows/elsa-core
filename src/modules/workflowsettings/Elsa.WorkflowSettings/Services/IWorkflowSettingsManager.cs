using System.Threading;
using System.Threading.Tasks;
using Elsa.WorkflowSettings.Models;

namespace Elsa.WorkflowSettings.Services
{
    public interface IWorkflowSettingsManager
    {
        Task<WorkflowSetting> LoadSettingAsync(string workflowBlueprintId, string key, CancellationToken cancellationToken = default);
    }
}