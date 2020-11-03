using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public abstract class WorkflowContextLoader<T> : WorkflowContextProvider<T>, ILoadWorkflowContext
    {
        public abstract ValueTask<object?> LoadAsync(LoadWorkflowContext context, CancellationToken cancellationToken = default);
    }
    
    public abstract class WorkflowContextLoader : WorkflowContextProvider, ILoadWorkflowContext
    {
        public abstract ValueTask<object?> LoadAsync(LoadWorkflowContext context, CancellationToken cancellationToken = default);
    }
}