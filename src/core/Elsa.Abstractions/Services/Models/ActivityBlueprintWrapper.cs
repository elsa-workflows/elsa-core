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
    }

    public class ActivityBlueprintWrapper<TActivity> : ActivityBlueprintWrapper, IActivityBlueprintWrapper<TActivity> where TActivity : IActivity
    {
        public ActivityBlueprintWrapper(ActivityExecutionContext activityExecutionContext) : base(activityExecutionContext)
        {
        }

        public async ValueTask<T> GetPropertyValueAsync<T>(Expression<Func<TActivity, T>> propertyExpression, CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = ActivityExecutionContext.WorkflowExecutionContext.WorkflowBlueprint;
            var activityId = ActivityExecutionContext.ActivityBlueprint.Id;
            return await workflowBlueprint.GetActivityPropertyValue(activityId, propertyExpression, ActivityExecutionContext, cancellationToken);
        }

        public T? GetState<T>(Expression<Func<TActivity, T>> propertyExpression)
        {
            var workflowBlueprint = ActivityExecutionContext.WorkflowExecutionContext.WorkflowBlueprint;
            return workflowBlueprint.GetActivityState(propertyExpression, ActivityExecutionContext);
        }
    }
}