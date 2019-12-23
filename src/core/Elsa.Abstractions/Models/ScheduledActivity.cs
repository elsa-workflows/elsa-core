namespace Elsa.Models
{
    public class ScheduledActivity
    {
        public ScheduledActivity()
        {
        }

        public ScheduledActivity(string activityId, Variable? input = default)
        {
            ActivityId = activityId;
            Input = input;
        }
        
        public string? ActivityId { get; set; }
        public Variable? Input { get; set; }
    }
}