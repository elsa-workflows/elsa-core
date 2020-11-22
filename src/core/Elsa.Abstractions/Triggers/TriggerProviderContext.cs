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
        public ActivityBlueprintWrapper<TActivity> GetActivity<TActivity>() where TActivity : IActivity => new(ActivityExecutionContext);
    }

    public class TriggerProviderContext<T> : TriggerProviderContext where T:IActivity
    {
        public TriggerProviderContext(ActivityExecutionContext activityExecutionContext) : base(activityExecutionContext)
        {
        }

        public ActivityBlueprintWrapper<T> Activity => GetActivity<T>();
    }
}