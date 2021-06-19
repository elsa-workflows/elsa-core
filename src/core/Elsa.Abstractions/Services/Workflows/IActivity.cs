using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IActivity
    {
        /// <summary>
        /// The type name of this activity.
        /// </summary>
        string Type { get;}
        
        /// <summary>
        /// Unique identifier of this activity within the workflow.
        /// </summary>
        string Id { get; set; }
        
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
        /// A value indicating whether the workflow instance will be persisted automatically upon executing this activity.
        /// </summary>
        bool PersistWorkflow { get; set; }
        
        /// <summary>
        /// A value indicating whether the workflow context (if any) will be refreshed automatically before executing this activity. 
        /// </summary>
        bool LoadWorkflowContext { get; set; }
        
        /// <summary>
        /// A value indicating whether the workflow context (if any) will be persisted automatically after executing this activity. 
        /// </summary>
        bool SaveWorkflowContext { get; set; }
        
        /// <summary>
        /// A data store for the activity to store information that needs to be persisted as part of the workflow instance.
        /// </summary>
        IDictionary<string, object?> Data { get; set; }

        /// <summary>
        /// Returns a value of whether the specified activity can execute.
        /// </summary>
        ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context);

        /// <summary>
        /// Executes the specified activity.
        /// </summary>
        ValueTask<IActivityExecutionResult> ExecuteAsync(ActivityExecutionContext context);

        /// <summary>
        /// Resumes the specified activity.
        /// </summary>
        ValueTask<IActivityExecutionResult> ResumeAsync(ActivityExecutionContext context);
    }
}