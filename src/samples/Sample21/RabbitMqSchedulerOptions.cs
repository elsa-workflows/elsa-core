using Elsa.Activities.MassTransit.Options;

namespace Sample21
{
    public class RabbitMqSchedulerOptions : RabbitMqOptions
    {
        public MessageScheduleOptions MessageSchedule { get; set; }
    }
}