using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowHost
    {
        /// <summary>
        /// Starts new workflows that start with the specified activity name and resumes halted workflows that are blocked on activities with the specified activity name.
        /// </summary>
        Task TriggerWorkflowsAsync(string activityType, Variables input, CancellationToken cancellationToken = default);
    }
}