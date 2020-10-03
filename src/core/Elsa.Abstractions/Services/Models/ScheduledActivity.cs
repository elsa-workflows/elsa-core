namespace Elsa.Services.Models
{
    public class ScheduledActivity
    {
        public ScheduledActivity(IActivity activity, object? input = default)
        {
            Activity = activity;
            Input = input;
        }
        
        
        public IActivity Activity { get; }
        public object? Input { get; }
    }
}