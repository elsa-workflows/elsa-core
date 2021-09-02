using System.Threading;
using System.Threading.Tasks;
using Elsa.WorkflowSettings.Models;

namespace Elsa.WorkflowSettings.Services.WorkflowSettings
{
    public interface IWorkflowSettingsManager
    {
        Task<WorkflowSetting> LoadSettingAsync(string workflowBlueprintId, string key, CancellationToken cancellationToken = default);
    }
}