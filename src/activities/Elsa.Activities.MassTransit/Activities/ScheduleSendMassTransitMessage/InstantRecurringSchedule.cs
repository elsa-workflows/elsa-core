using MassTransit.Scheduling;
using NodaTime;

namespace Elsa.Activities.MassTransit.Activities.ScheduleSendMassTransitMessage
{
    public class InstantRecurringSchedule : DefaultRecurringSchedule
    {
        public InstantRecurringSchedule(Instant startTime)
        {
            StartTime = startTime.ToDateTimeOffset();
        }
    }
}