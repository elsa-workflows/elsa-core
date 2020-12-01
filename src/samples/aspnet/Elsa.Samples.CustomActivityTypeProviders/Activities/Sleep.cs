using Elsa.Activities.Timers;
using Elsa.Activities.Timers.Services;
using Elsa.Services;
using NodaTime;

namespace Elsa.Samples.CustomActivityTypeProviders.Activities
{
    public class Sleep : TimerEvent
    {
        public Sleep(IWorkflowInstanceManager workflowInstanceManager, IWorkflowScheduler workflowScheduler, IClock clock) : base(workflowInstanceManager, workflowScheduler, clock)
        {
        }
    }
}