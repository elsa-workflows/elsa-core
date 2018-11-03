using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;

namespace Flowsharp
{
    public interface IWorkflowInvoker
    {
        IActivityInvoker ActivityInvoker { get; }
        Task<WorkflowExecutionContext> InvokeAsync(Workflow workflow, IActivity startActivity = default, Variables arguments = default, CancellationToken cancellationToken = default);
        Task<WorkflowExecutionContext> ResumeAsync(Workflow workflow, IActivity startActivity = default, Variables arguments = default, CancellationToken cancellationToken = default);
    }
}
