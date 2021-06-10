using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services.Bookmarks
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
    }

    public class BookmarkProviderContext<TActivity> : BookmarkProviderContext where TActivity: IActivity
    {
        public BookmarkProviderContext(ActivityExecutionContext activityExecutionContext, ActivityType activityType, BookmarkIndexingMode mode) : base(activityExecutionContext, activityType, mode)
        {
        }

        public IActivityBlueprintWrapper<TActivity> Activity => GetActivity<TActivity>();
        
        public async ValueTask<T?> ReadActivityPropertyAsync<T>(Expression<Func<TActivity, T>> propertyExpression, CancellationToken cancellationToken = default)
        {
            var activityBlueprint = GetActivity<TActivity>();

            if (Mode == BookmarkIndexingMode.WorkflowBlueprint)
                return await activityBlueprint.EvaluatePropertyValueAsync(propertyExpression, cancellationToken);
            
            return activityBlueprint.GetPropertyValue(propertyExpression);
        }
    }
}