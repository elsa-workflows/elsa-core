using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IActivity
    {
        /// <summary>
        /// Holds persistable activity state.
        /// </summary>
        Variables State { get; set; }
        
        /// <summary>
        /// Holds activity output.
        /// </summary>
        Variable? Output { get; set; }
        
        /// <summary>
        /// The type name of this activity.
        /// </summary>
        string Type { get; }
        
        /// <summary>
        /// Unique identifier of this activity.
        /// </summary>
        string? Id { get; set; }
        
        /// <summary>
        /// Name identifier of this activity.
        /// </summary>
        string? Name { get; set; }
        
        /// <summary>
        /// Display name of this activity.
        /// </summary>
        string? DisplayName { get; set; }
        
        /// <summary>
        /// Description of this activity.
        /// </summary>
        string? Description { get; set; }
        
        /// <summary>
        /// Returns a value of whether the specified activity can execute.
        /// </summary>
        Task<bool> CanExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the specified activity.
        /// </summary>
        Task<IActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resumes the specified activity.
        /// </summary>
        Task<IActivityExecutionResult> ResumeAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken = default);

        Task<IActivityExecutionResult> ChildActivityExecutedAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, ActivityExecutionContext childActivityExecutionContext, CancellationToken cancellationToken = default);
    }
}