using Elsa.Models;

namespace Elsa.Services.Models
{
    public class ScheduledActivity
    {
        public ScheduledActivity(IActivity activity, object? input = default) : this(activity, Variable.From(input))
        {
        }
        
        public ScheduledActivity(IActivity activity, Variable? input = default)
        {
            Activity = activity;
            Input = input;
        }
        
        public IActivity? Activity { get; }
        public Variable? Input { get; }
    }
}