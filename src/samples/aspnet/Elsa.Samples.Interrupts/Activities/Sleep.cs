using Elsa.Activities.Timers;
using Elsa.Activities.Timers.Services;
using Elsa.Attributes;
using Elsa.Repositories;
using Elsa.Services;
using NodaTime;

namespace Elsa.Samples.Interrupts.Activities
{
    [Activity]
    public class Sleep : Timer
    {
        public Sleep(IWorkflowInstanceRepository workflowInstanceRepository, IWorkflowScheduler workflowScheduler, IClock clock) : base(clock)
        {
        }
    }
}