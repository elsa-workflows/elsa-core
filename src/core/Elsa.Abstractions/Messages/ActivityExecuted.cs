using Elsa.Services.Models;

namespace Elsa.Messages
{
    public class ActivityExecuted : ProcessNotification
    {
        public ActivityExecuted(ProcessInstance process, IActivity activity) : base(process)
        {
            Activity = activity;
        }
        
        public IActivity Activity { get; }
    }
}