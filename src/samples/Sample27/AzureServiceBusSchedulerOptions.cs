using Elsa.Activities.MassTransit.Options;

namespace Sample27
{
    public class AzureServiceBusSchedulerOptions : AzureServiceBusOptions
    {
        public MessageScheduleOptions MessageSchedule { get; set; }
    }
}