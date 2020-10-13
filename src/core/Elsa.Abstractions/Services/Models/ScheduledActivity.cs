using Elsa.Models;

namespace Elsa.Services.Models
{
    public class ScheduledActivity : IScheduledActivity
    {
        public ScheduledActivity(string activityId, object? input = default)
        {
            ActivityId = activityId;
            Input = input;
        }

        public string ActivityId { get; }
        public object? Input { get; }
    }
}