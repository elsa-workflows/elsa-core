using System.Threading;
using System.Threading.Tasks;
using Elsa.Results;
using Elsa.Services.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.Services
{
    public interface IActivity
    {
        /// <summary>
        /// Holds persistable activity state.
        /// </summary>
        JObject State { get; set; }
        
        /// <summary>
        /// The type name of this activity.
        /// </summary>
        string TypeName { get; }
        
        /// <summary>
        /// Unique identifier of this activity
        /// </summary>
        string Id { get; set; }
        string Alias { get; set; }
        /// <summary>
        /// The activity type of this driver.
        /// </summary>
        
        /// <summary>
        /// Returns a value of whether the specified activity can execute.
        /// </summary>
        Task<bool> CanExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the specified activity.
        /// </summary>
        Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resumes the specified activity.
        /// </summary>
        Task<ActivityExecutionResult> ResumeAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Invoked when the workflow is halted.
        /// </summary>
        Task<ActivityExecutionResult> HaltedAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default);
    }
}