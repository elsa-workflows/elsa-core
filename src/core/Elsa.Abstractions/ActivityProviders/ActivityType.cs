using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.ActivityProviders
{
    public class ActivityType
    {
        /// <summary>
        /// The type name of this activity.
        /// </summary>
        public string Type { get; set; } = default!;

        /// <summary>
        /// Display name of this activity.
        /// </summary>
        public string DisplayName { get; set; } = default!;

        /// <summary>
        /// Description of this activity.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Anything you want to store with this activity type. 
        /// </summary>
        public IDictionary<string, object> Annotations { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Returns a value of whether the specified activity can execute.
        /// </summary>
        public Func<ActivityExecutionContext, ValueTask<bool>> CanExecuteAsync { get; set; } = _ => new ValueTask<bool>(true);

        /// <summary>
        /// Executes the activity.
        /// </summary>
        public Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> ExecuteAsync { get; set; } = _ => new ValueTask<IActivityExecutionResult>(new DoneResult());

        /// <summary>
        /// Resumes the specified activity.
        /// </summary>
        public Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> ResumeAsync { get; set; } = _ => new ValueTask<IActivityExecutionResult>(new DoneResult());

        public Func<ActivityExecutionContext, ValueTask<IActivity>> ActivateAsync { get; set; } = _ => new ValueTask<IActivity>();
    }
}