namespace Elsa.Services.Models
{
    public class ActivityExecutionContext
    {
        public ActivityExecutionContext(IActivity activity)
        {
            Activity = activity;
        }

        public IActivity Activity { get; }
    }
}
