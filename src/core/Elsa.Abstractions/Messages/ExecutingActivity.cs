using Elsa.Services.Models;

namespace Elsa.Messages
{
    public class ExecutingActivity : WorkflowNotification
    {
        public ExecutingActivity(Workflow workflow, IActivity activity) : base(workflow)
        {
            Activity = activity;
        }
        
        public IActivity Activity { get; }
    }
}