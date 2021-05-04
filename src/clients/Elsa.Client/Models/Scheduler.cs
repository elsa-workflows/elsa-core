using System.Collections.Generic;
using System.Linq;

namespace Elsa.Client.Models
{
    public class Scheduler
    {
        private readonly Stack<ScheduledActivity> _stack = new Stack<ScheduledActivity>();

        public bool HasScheduledActivities => _stack.Any();

        public ScheduledActivity Schedule(string activityId, object? input = null)
        {
            var scheduledActivity = new ScheduledActivity(activityId, input);
            _stack.Push(scheduledActivity);
            return scheduledActivity;
        }

        public ScheduledActivity Next() => _stack.Pop();
    }
}