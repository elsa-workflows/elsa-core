using Elsa.Services.Models;

namespace Elsa.Messages
{
    public class ActivityExecuted : WorkflowNotification
    {
        public ActivityExecuted(Workflow workflow, IActivity activity) : base(workflow)
        {
            Activity = activity;
        }
        
        public IActivity Activity { get; }
    }
}