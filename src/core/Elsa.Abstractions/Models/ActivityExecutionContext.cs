namespace Elsa.Models
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
}
