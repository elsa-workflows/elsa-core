using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities;
using Flowsharp.Models;
using Flowsharp.Results;

namespace Flowsharp
{
    public interface IActivityInvoker
    {
        Task<bool> CanExecuteAsync(IActivity activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken);
        Task<ActivityExecutionResult> ExecuteAsync(IActivity activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken);
        Task<ActivityExecutionResult> ResumeAsync(IActivity activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken);
    }
}