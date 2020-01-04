using Elsa.Models;

namespace Elsa.Services.Models
{
    public class ActivityExecutionContext
    {
        public ActivityExecutionContext(IActivity activity, Variable? input = null)
        {
            Activity = activity;
            Input = input;
        }

        public IActivity Activity { get; }
        public Variable? Input { get; }
    }
}
