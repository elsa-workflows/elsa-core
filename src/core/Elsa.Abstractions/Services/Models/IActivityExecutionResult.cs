using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services.Models
{
    public interface IActivityExecutionResult
    {
        Task ExecuteAsync(IWorkflowInvoker invoker, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken);
    }
}
