using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public class BookmarkProviderContext
    {
        public BookmarkProviderContext(ActivityExecutionContext activityExecutionContext, ActivityType activityType, BookmarkIndexingMode mode)
        {
            ActivityExecutionContext = activityExecutionContext;
            ActivityType = activityType;
            Mode = mode;
        }

        public ActivityExecutionContext ActivityExecutionContext { get; }
        public ActivityType ActivityType { get; }
        public BookmarkIndexingMode Mode { get; }
        public IActivityBlueprintWrapper<TActivity> GetActivity<TActivity>() where TActivity : IActivity => new ActivityBlueprintWrapper<TActivity>(ActivityExecutionContext);
        
        public async ValueTask<T?> ReadActivityPropertyAsync<TActivity, T>(Expression<Func<TActivity, T>> propertyExpression, CancellationToken cancellationToken = default) where TActivity : IActivity
        {
            var activityBlueprint = GetActivity<TActivity>();

            if (Mode == BookmarkIndexingMode.WorkflowBlueprint)
                return await activityBlueprint.EvaluatePropertyValueAsync(propertyExpression, cancellationToken);
            
            return activityBlueprint.GetPropertyValue(propertyExpression);
        }
    }

    public class BookmarkProviderContext<TActivity> : BookmarkProviderContext where TActivity: IActivity
    {
        public BookmarkProviderContext(ActivityExecutionContext activityExecutionContext, ActivityType activityType, BookmarkIndexingMode mode) : base(activityExecutionContext, activityType, mode)
        {
        }

        public IActivityBlueprintWrapper<TActivity> Activity => GetActivity<TActivity>();

        public ValueTask<T?> ReadActivityPropertyAsync<T>(Expression<Func<TActivity, T>> propertyExpression, CancellationToken cancellationToken = default) => base.ReadActivityPropertyAsync(propertyExpression, cancellationToken);
    }
}