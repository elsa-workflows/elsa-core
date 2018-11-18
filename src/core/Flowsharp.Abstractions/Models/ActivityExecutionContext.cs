namespace Flowsharp.Models
{
    public class ActivityExecutionContext
    {
        public ActivityExecutionContext(Flowsharp.IActivity activity, ActivityDescriptor descriptor)
        {
            Activity = activity;
            Descriptor = descriptor;
        }

        public Flowsharp.IActivity Activity { get; }
        public ActivityDescriptor Descriptor { get; }
    }

    public class ActivityExecutionContext<T> : ActivityExecutionContext where T : Flowsharp.IActivity
    {
        public ActivityExecutionContext(Flowsharp.IActivity activity, ActivityDescriptor descriptor) : base(activity, descriptor)
        {
        }

        public new T Activity => (T) base.Activity;
    }
}
