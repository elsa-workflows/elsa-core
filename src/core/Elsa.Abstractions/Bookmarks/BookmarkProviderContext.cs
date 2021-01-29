using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Bookmarks
{
    public class BookmarkProviderContext
    {
        public BookmarkProviderContext(ActivityExecutionContext activityExecutionContext)
        {
            ActivityExecutionContext = activityExecutionContext;
        }

        public ActivityExecutionContext ActivityExecutionContext { get; }
        public IActivityBlueprintWrapper<TActivity> GetActivity<TActivity>() where TActivity : IActivity => new ActivityBlueprintWrapper<TActivity>(ActivityExecutionContext);
    }

    public class BookmarkProviderContext<T> : BookmarkProviderContext where T: IActivity
    {
        public BookmarkProviderContext(ActivityExecutionContext activityExecutionContext) : base(activityExecutionContext)
        {
        }

        public IActivityBlueprintWrapper<T> Activity => GetActivity<T>();
    }
}