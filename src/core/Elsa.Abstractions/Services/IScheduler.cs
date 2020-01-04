using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IScheduler
    {
        Task<WorkflowExecutionContext> ScheduleActivityAsync(
            IActivity activity,
            object? input = default,
            CancellationToken cancellationToken = default);
    }
}