using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities;
using Flowsharp.Models;

namespace Flowsharp.Services
{
    public interface IWorkflowInvoker
    {
        IActivityInvoker ActivityInvoker { get; }
        Task<WorkflowExecutionContext> InvokeAsync(Workflow workflow, IActivity startActivity = default, CancellationToken cancellationToken = default);
    }
}
