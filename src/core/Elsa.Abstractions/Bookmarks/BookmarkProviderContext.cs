using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Bookmarks
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

    public class BookmarkProviderContext<T> : BookmarkProviderContext where T: IActivity
    {
        public BookmarkProviderContext(ActivityExecutionContext activityExecutionContext, ActivityType activityType, BookmarkIndexingMode mode) : base(activityExecutionContext, activityType, mode)
        {
        }

        public IActivityBlueprintWrapper<T> Activity => GetActivity<T>();
    }
}