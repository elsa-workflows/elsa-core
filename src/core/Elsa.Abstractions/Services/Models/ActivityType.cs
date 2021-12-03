using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Services.Models
{
    public class ActivityType
    {
        public ActivityType()
        {
            ActivateAsync = context => new ValueTask<IActivity>((IActivity)context.ServiceProvider.GetRequiredService(Type));
        }

        /// <summary>
        /// The type name of this activity.
        /// </summary>
        public string TypeName { get; set; } = default!;

        /// <summary>
        /// The .NET Runtime type of this activity.
        /// </summary>
        public Type Type { get; set; } = typeof(IActivity);

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

        public Func<ActivityExecutionContext, ValueTask<IActivity>> ActivateAsync { get; set; }

        /// <summary>
        /// Returns a value of whether the specified activity can execute.
        /// </summary>
        public Func<ActivityExecutionContext, IActivity, ValueTask<bool>> CanExecuteAsync { get; set; } = (_, _) => new ValueTask<bool>(true);

        /// <summary>
        /// Executes the activity.
        /// </summary>
        public Func<ActivityExecutionContext, IActivity, ValueTask<IActivityExecutionResult>> ExecuteAsync { get; set; } = (_, _) => new ValueTask<IActivityExecutionResult>(new DoneResult());

        /// <summary>
        /// Resumes the specified activity.
        /// </summary>
        public Func<ActivityExecutionContext, IActivity, ValueTask<IActivityExecutionResult>> ResumeAsync { get; set; } = (_, _) => new ValueTask<IActivityExecutionResult>(new DoneResult());

        public Func<ValueTask<ActivityDescriptor>> DescribeAsync { get; set; } = () => new ValueTask<ActivityDescriptor>(new ActivityDescriptor());
        public bool IsBrowsable { get; set; } = true;

        public IDictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

        public override string ToString() => TypeName;
    }
}