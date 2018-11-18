namespace Flowsharp.Models
{
    public class ActivityExecutionContext
    {
        public ActivityExecutionContext(Flowsharp.IActivity activity, IActivity descriptor)
        {
            Activity = activity;
            Descriptor = descriptor;
        }

        public Flowsharp.IActivity Activity { get; }
        public IActivity Descriptor { get; }
    }

    public class ActivityExecutionContext<T> : ActivityExecutionContext where T : Flowsharp.IActivity
    {
        public ActivityExecutionContext(Flowsharp.IActivity activity, IActivity descriptor) : base(activity, descriptor)
        {
        }

        public new T Activity => (T) base.Activity;
    }
}
