using Elsa.Services.Models;

namespace Elsa.Messages
{
    public class ExecutingActivity : ProcessNotification
    {
        public ExecutingActivity(ProcessInstance process, IActivity activity) : base(process)
        {
            Activity = activity;
        }
        
        public IActivity Activity { get; }
    }
}