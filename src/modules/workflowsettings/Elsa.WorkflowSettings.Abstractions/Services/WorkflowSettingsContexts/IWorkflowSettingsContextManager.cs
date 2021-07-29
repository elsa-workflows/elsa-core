using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.WorkflowSettings.Abstractions.Services.WorkflowSettingsContexts
{
    public interface IWorkflowSettingsContextManager
    {
        ValueTask<bool> LoadContext(WorkflowSettingsContext context, CancellationToken cancellationToken = default);
    }
}