using Elsa.Persistence.EntityFramework.Core.Services;

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.Core.Services
{
    public interface IWorkflowSettingsContextFactory : IContextFactory<WorkflowSettingsContext>
    {
    }
}