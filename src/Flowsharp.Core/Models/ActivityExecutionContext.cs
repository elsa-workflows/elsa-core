using Flowsharp.Activities;

namespace Flowsharp.Models
{
    public class ActivityExecutionContext
    {
        public ActivityExecutionContext(IActivity activity)
        {
            Activity = activity;
        }

        public IActivity Activity { get; private set; }
    }
}
