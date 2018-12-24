using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Results;
using Microsoft.Extensions.Localization;

namespace Flowsharp
{
    public interface IActivityHandler
    {
        /// <summary>
        /// The type of activity handled by this handler.
        /// </summary>
        Type ActivityType { get; }

        /// <summary>
        /// The category of the activity.
        /// </summary>
        LocalizedString Category { get; }
        
        /// <summary>
        /// The friendly name of the activity.
        /// </summary>
        LocalizedString DisplayText { get; }
        
        /// <summary>
        /// A brief description of the activity. Used by tooling.
        /// </summary>
        LocalizedString Description { get; }

        /// <summary>
        /// Returns the available endpoints that can be triggered.
        /// </summary>
        /// <returns></returns>
        IEnumerable<LocalizedString> GetEndpoints();
        
        /// <summary>
        /// Returns a value of whether the specified activity can execute.
        /// </summary>
        Task<bool> CanExecuteAsync(ActivityExecutionContext activityContext, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken);

        /// <summary>
        /// Executes the specified activity.
        /// </summary>
        Task<ActivityExecutionResult> ExecuteAsync(ActivityExecutionContext activityContext, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken);

        /// <summary>
        /// Resumes the specified activity.
        /// </summary>
        Task<ActivityExecutionResult> ResumeAsync(ActivityExecutionContext activityContext, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken);
    }
}