using System.Threading;
using System.Threading.Tasks;
using Elsa.Results;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IActivityInvoker
    {
        Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, IActivity activity, CancellationToken cancellationToken = default);
        Task<ActivityExecutionResult> HaltedAsync(WorkflowExecutionContext workflowContext, IActivity activity, CancellationToken cancellationToken = default);
    }
}