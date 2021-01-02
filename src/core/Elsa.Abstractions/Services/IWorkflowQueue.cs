using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services
{
    public interface IWorkflowQueue
    {
        /// <summary>
        /// Enqueues the specified workflow instance and activity for execution in the background.
        /// </summary>
        Task Enqueue(string workflowInstanceId, string activityId, CancellationToken cancellationToken = default);
    }
}