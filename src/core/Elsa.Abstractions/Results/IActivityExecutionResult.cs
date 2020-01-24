using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Results
{
    public interface IActivityExecutionResult
    {
        Task ExecuteAsync(ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken);
    }
}