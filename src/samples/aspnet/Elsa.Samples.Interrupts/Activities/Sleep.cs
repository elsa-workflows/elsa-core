using Elsa.Activities.Timers;
using Elsa.Activities.Timers.Services;
using Elsa.Attributes;
using Elsa.Services;
using NodaTime;

namespace Elsa.Samples.Interrupts.Activities
{
    [Activity]
    public class Sleep : Timer
    {
        public Sleep(IWorkflowInstanceManager workflowInstanceManager, IWorkflowScheduler workflowScheduler, IClock clock) : base(workflowInstanceManager, workflowScheduler, clock)
        {
        }
    }
}