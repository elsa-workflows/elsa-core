using System.Threading;
using System.Threading.Tasks;
using Elsa.Providers.WorkflowContexts;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public abstract class WorkflowContextPersister<T> : WorkflowContextProvider<T>, ISaveWorkflowContext
    {
        public abstract ValueTask<string?> SaveAsync(SaveWorkflowContext context, CancellationToken cancellationToken = default);
    }
    
    public abstract class WorkflowContextPersister : WorkflowContextProvider, ISaveWorkflowContext
    {
        public abstract ValueTask<string?> SaveAsync(SaveWorkflowContext context, CancellationToken cancellationToken = default);
    }
}