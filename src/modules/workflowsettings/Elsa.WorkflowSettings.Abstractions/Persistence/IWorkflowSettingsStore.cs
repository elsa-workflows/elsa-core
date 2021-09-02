using Elsa.Persistence;
using Elsa.WorkflowSettings.Models;

namespace Elsa.WorkflowSettings.Abstractions.Persistence
{
    public interface IWorkflowSettingsStore : IStore<WorkflowSetting>
    {
    }
}
