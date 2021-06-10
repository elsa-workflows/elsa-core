using System.Threading;
using System.Threading.Tasks;
using Elsa.Providers.WorkflowContext;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface ILoadWorkflowContext : IWorkflowContextProvider
    {
        ValueTask<object?> LoadAsync(LoadWorkflowContext context, CancellationToken cancellationToken = default);
    }
}