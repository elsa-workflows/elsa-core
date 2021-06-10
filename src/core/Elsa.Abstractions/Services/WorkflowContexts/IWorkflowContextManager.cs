using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowContextManager
    {
        ValueTask<object?> LoadContext(LoadWorkflowContext context, CancellationToken cancellationToken = default);
        ValueTask<string?> SaveContextAsync(SaveWorkflowContext context, CancellationToken cancellationToken = default);
    }
}