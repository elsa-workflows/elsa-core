using System.Threading;
using System.Threading.Tasks;
using Elsa.WorkflowSettings.Models;
using Elsa.WorkflowSettings.Persistence;

namespace Elsa.WorkflowSettings.Providers
{
    /// <summary>
    /// Represents a source of workflow settings for the <see cref="IWorkflowSettingsStore"/>
    /// </summary>
    public interface IWorkflowSettingsProvider
    {
        ValueTask<WorkflowSetting> GetWorkflowSettingAsync(string workflowBlueprintId, string key, CancellationToken cancellationToken);
    }
}