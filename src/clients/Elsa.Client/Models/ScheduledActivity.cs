namespace Elsa.Client.Models
{
    public class ScheduledActivity
    {
        public ScheduledActivity()
        {
        }

        public ScheduledActivity(string activityId, object? input = default)
        {
            ActivityId = activityId;
            Input = input;
        }

        public string ActivityId { get; set; } = default!;
        public object? Input { get; set; }
    }
}