using Elsa.Activities.Temporal;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.Attributes;
using Elsa.Persistence;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Elsa.Samples.Interrupts.Activities
{
    [Activity]
    public class Sleep : Timer
    {
        public Sleep(IWorkflowInstanceStore workflowInstanceStore, IWorkflowScheduler workflowScheduler, IClock clock, ILogger<Sleep> logger) : base(clock, logger)
        {
        }
    }
}