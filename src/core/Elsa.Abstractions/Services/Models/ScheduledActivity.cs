using Elsa.Models;

namespace Elsa.Services.Models
{
    public class ScheduledActivity
    {
        public ScheduledActivity(IActivity activity, object? input = default)
        {
            Activity = activity;
            Input = Variable.From(input);
        }
        
        public IActivity? Activity { get; }
        public Variable? Input { get; }
    }
}