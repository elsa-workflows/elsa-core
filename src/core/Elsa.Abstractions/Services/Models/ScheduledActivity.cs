using Elsa.Models;

namespace Elsa.Services.Models
{
    public class ScheduledActivity
    {
        public ScheduledActivity()
        {
        }

        public ScheduledActivity(IActivity activity, Variable? input = default)
        {
            Activity = activity;
            Input = input;
        }
        
        public IActivity? Activity { get; set; }
        public Variable? Input { get; set; }
    }
}