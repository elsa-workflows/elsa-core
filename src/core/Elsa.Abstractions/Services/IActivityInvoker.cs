using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IActivityInvoker
    {
        Task<IActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, IActivity activity, Variable? input = null, CancellationToken cancellationToken = default);
        Task<IActivityExecutionResult> ResumeAsync(WorkflowExecutionContext workflowContext, IActivity activity, Variable? input = null, CancellationToken cancellationToken = default);
    }
}