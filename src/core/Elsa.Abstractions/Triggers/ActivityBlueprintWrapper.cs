using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Triggers
{
    public class ActivityBlueprintWrapper<TActivity> where TActivity : IActivity
    {
        private readonly ActivityExecutionContext _activityExecutionContext;

        public ActivityBlueprintWrapper(ActivityExecutionContext activityExecutionContext)
        {
            _activityExecutionContext = activityExecutionContext;
        }
        
        public async ValueTask<T> GetPropertyValueAsync<T>(Expression<Func<TActivity, T>> propertyExpression, CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = _activityExecutionContext.WorkflowExecutionContext.WorkflowBlueprint;
            var activityId = _activityExecutionContext.ActivityBlueprint.Id;
            return await workflowBlueprint.GetActivityPropertyValue(activityId, propertyExpression, _activityExecutionContext, cancellationToken);
        }
        
        public T GetState<T>(Expression<Func<TActivity, T>> propertyExpression)
        {
            var workflowBlueprint = _activityExecutionContext.WorkflowExecutionContext.WorkflowBlueprint;
            return workflowBlueprint.GetActivityState(propertyExpression, _activityExecutionContext);
        }
    }
}