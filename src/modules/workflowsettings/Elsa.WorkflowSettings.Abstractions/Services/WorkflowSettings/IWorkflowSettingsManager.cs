using System.Threading;
using System.Threading.Tasks;
using Elsa.WorkflowSettings.Models;

namespace Elsa.WorkflowSettings.Abstractions.Services.WorkflowSettings
{
    public interface IWorkflowSettingsManager
    {
        ValueTask<WorkflowSetting> LoadSettingAsync(WorkflowSetting workflowSetting, CancellationToken cancellationToken = default);
    }
}