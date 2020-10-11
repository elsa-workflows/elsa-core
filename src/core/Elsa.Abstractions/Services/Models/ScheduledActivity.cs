using Elsa.Models;

namespace Elsa.Services.Models
{
    public class ScheduledActivity : IScheduledActivity
    {
        public ScheduledActivity(ActivityDefinition activityDefinition, object? input = default)
        {
            ActivityDefinition = activityDefinition;
            Input = input;
        }

        public ActivityDefinition ActivityDefinition { get; }
        public object? Input { get; }
    }
}