using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Results
{
    public interface IActivityExecutionResult
    {
        Task ExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken);
    }
}