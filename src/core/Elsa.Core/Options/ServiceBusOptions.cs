using System;
using Humanizer;
using Rebus.Config;

namespace Elsa.Options
{
    public class ServiceBusOptions
    {
        public static string FormatChannelQueueName<TMessage>(string channel) => FormatChannelQueueName(typeof(TMessage), channel);
        public static string FormatChannelQueueName(Type messageType, string channel) => FormatChannelQueueName(messageType.Name, channel);

        public static string FormatChannelQueueName(string queueName, string channel)
        {
            var queue = !string.IsNullOrWhiteSpace(channel) ? $"{queueName}{channel}" : queueName;
            return FormatQueueName(queue);
        }

        public static string FormatQueueName(string queue) => queue.Humanize().Dehumanize().Underscore().Dasherize();

        public int? NumberOfWorkers { get; set; }
        public int? MaxParallelism { get; set; }
        public string? QueuePrefix { get; set; }

        public void Apply(OptionsConfigurer configurer)
        {
            if (NumberOfWorkers != null)
                configurer.SetNumberOfWorkers(NumberOfWorkers.Value);

            if (MaxParallelism != null)
                configurer.SetMaxParallelism(MaxParallelism.Value);
        }
    }
}