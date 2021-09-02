using Elsa.Persistence;
using Elsa.WorkflowSettings.Models;

namespace Elsa.WorkflowSettings.Persistence
{
    public interface IWorkflowSettingsStore : IStore<WorkflowSetting>
    {
    }
}
