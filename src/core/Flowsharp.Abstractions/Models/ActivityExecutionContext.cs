namespace Flowsharp.Models
{
    public class ActivityExecutionContext
    {
        public ActivityExecutionContext(IActivity activity, ActivityDescriptor descriptor)
        {
            Activity = activity;
            Descriptor = descriptor;
        }

        public IActivity Activity { get; }
        public ActivityDescriptor Descriptor { get; }
    }

    public class ActivityExecutionContext<T> : ActivityExecutionContext where T : IActivity
    {
        public ActivityExecutionContext(IActivity activity, ActivityDescriptor descriptor) : base(activity, descriptor)
        {
        }

        public new T Activity => (T) base.Activity;
    }
}
