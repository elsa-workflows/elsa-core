using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Results;
using Microsoft.Extensions.Localization;

namespace Flowsharp
{
    public interface IActivityInvoker
    {
        Task<bool> CanExecuteAsync(IActivity activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken);
        IEnumerable<LocalizedString> GetEndpoints(IActivity activity);
        Task<ActivityExecutionResult> ExecuteAsync(IActivity activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken);
        Task<ActivityExecutionResult> ResumeAsync(IActivity activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken);
    }
}