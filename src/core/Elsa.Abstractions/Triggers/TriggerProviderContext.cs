using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Triggers
{
    public class TriggerProviderContext
    {
        public TriggerProviderContext(ActivityExecutionContext activityExecutionContext)
        {
            ActivityExecutionContext = activityExecutionContext;
        }

        public ActivityExecutionContext ActivityExecutionContext { get; }
        public IActivityBlueprintWrapper<TActivity> GetActivity<TActivity>() where TActivity : IActivity => new ActivityBlueprintWrapper<TActivity>(ActivityExecutionContext);
    }

    public class TriggerProviderContext<T> : TriggerProviderContext where T: IActivity
    {
        public TriggerProviderContext(ActivityExecutionContext activityExecutionContext) : base(activityExecutionContext)
        {
        }

        public IActivityBlueprintWrapper<T> Activity => GetActivity<T>();
    }
}