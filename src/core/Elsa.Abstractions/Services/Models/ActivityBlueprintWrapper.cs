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
        
        public async ValueTask<object?> GetPropertyValueAsync(string propertyName, CancellationToken cancellationToken = default)
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

        public async ValueTask<T?> GetPropertyValueAsync<T>(Expression<Func<TActivity, T>> propertyExpression, CancellationToken cancellationToken = default)
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

        public T? GetState<T>(Expression<Func<TActivity, T>> propertyExpression) => ActivityExecutionContext.GetState(propertyExpression);
    }
}