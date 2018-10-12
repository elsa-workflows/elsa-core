using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;

namespace Flowsharp.ActivityResults
{
    public interface IActivityExecutionResult
    {
        Task ExecuteAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken);
    }
}
