using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services.Models
{
    public class ActivityBlueprintWrapper : IActivityBlueprintWrapper
    {
        protected ActivityExecutionContext ActivityExecutionContext { get; }

        public ActivityBlueprintWrapper(ActivityExecutionContext activityExecutionContext)
        {
            ActivityExecutionContext = activityExecutionContext;
        }

        public IActivityBlueprint ActivityBlueprint => ActivityExecutionContext.ActivityBlueprint;

        public IActivityBlueprintWrapper<TActivity> As<TActivity>() where TActivity : IActivity => new ActivityBlueprintWrapper<TActivity>(ActivityExecutionContext);
        
        public async ValueTask<object?> EvaluatePropertyValueAsync(string propertyName, CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = ActivityExecutionContext.WorkflowExecutionContext.WorkflowBlueprint;
            var activityId = ActivityExecutionContext.ActivityBlueprint.Id;

            // Computed property setters that depend on actual workflow state might fault, since we might be using a fake activity execution context.
            try
            {
                return await workflowBlueprint.GetActivityPropertyValue(activityId, propertyName, ActivityExecutionContext, cancellationToken);
            }
            catch (Exception)
            {
                return default;
            }
        }
    }

    public class ActivityBlueprintWrapper<TActivity> : ActivityBlueprintWrapper, IActivityBlueprintWrapper<TActivity> where TActivity : IActivity
    {
        public ActivityBlueprintWrapper(ActivityExecutionContext activityExecutionContext) : base(activityExecutionContext)
        {
        }

        /// <summary>
        /// Evaluates the property provider and returns the result.
        /// </summary>
        public async ValueTask<T?> EvaluatePropertyValueAsync<T>(Expression<Func<TActivity, T>> propertyExpression, CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = ActivityExecutionContext.WorkflowExecutionContext.WorkflowBlueprint;
            var activityId = ActivityExecutionContext.ActivityBlueprint.Id;

            // Computed property setters that depend on actual workflow state might fault, since we might be using a fake activity execution context.
            try
            {
                return await workflowBlueprint.GetActivityPropertyValue(activityId, propertyExpression, ActivityExecutionContext, cancellationToken);
            }
            catch (Exception)
            {
                return default;
            }
        }

        /// <summary>
        /// Retrieves the property value from the activity's State dictionary.
        /// </summary>
        public T? GetPropertyValue<T>(Expression<Func<TActivity, T>> propertyExpression) => ActivityExecutionContext.GetState(propertyExpression);
    }
}