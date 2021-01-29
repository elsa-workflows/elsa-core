using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Bookmarks
{
    public class BookmarkProviderContext
    {
        public BookmarkProviderContext(ActivityExecutionContext activityExecutionContext, BookmarkIndexingMode mode)
        {
            ActivityExecutionContext = activityExecutionContext;
            Mode = mode;
        }

        public ActivityExecutionContext ActivityExecutionContext { get; }
        public BookmarkIndexingMode Mode { get; }
        public IActivityBlueprintWrapper<TActivity> GetActivity<TActivity>() where TActivity : IActivity => new ActivityBlueprintWrapper<TActivity>(ActivityExecutionContext);
    }

    public class BookmarkProviderContext<T> : BookmarkProviderContext where T: IActivity
    {
        public BookmarkProviderContext(ActivityExecutionContext activityExecutionContext, BookmarkIndexingMode mode) : base(activityExecutionContext, mode)
        {
        }

        public IActivityBlueprintWrapper<T> Activity => GetActivity<T>();
    }
}