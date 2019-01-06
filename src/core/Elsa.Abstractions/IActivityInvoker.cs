using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Results;

namespace Elsa
{
    public interface IActivityInvoker
    {
        Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, IActivity activity, CancellationToken cancellationToken = default);
        Task<ActivityExecutionResult> ResumeAsync(WorkflowExecutionContext workflowContext, IActivity activity, CancellationToken cancellationToken = default);
        Task<ActivityExecutionResult> HaltedAsync(WorkflowExecutionContext workflowContext, IActivity activity, CancellationToken cancellationToken = default);
    }
}