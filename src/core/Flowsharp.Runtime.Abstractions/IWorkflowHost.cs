using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;

namespace Flowsharp.Runtime.Abstractions
{
    /// <summary>
    /// Provides higher-level workflow functionality, such as triggering new and halted workflows based on received stimuli.
    /// </summary>
    public interface IWorkflowHost
    {
        /// <summary>
        /// Starts new workflows that start with the specified activity name and resumes halted workflows that are blocked on activities with the specified activity name.
        /// </summary>
        Task TriggerWorkflowAsync(string activityName, Variables arguments, CancellationToken cancellationToken);
    }
}