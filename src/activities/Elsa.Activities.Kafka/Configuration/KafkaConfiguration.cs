using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;

namespace Elsa.Activities.Kafka.Configuration
{
    public class KafkaConfiguration
    {
        public string ConnectionString { get; }
        public string Topic { get; }
        public string Group { get; }
        public Confluent.Kafka.AutoOffsetReset AutoOffsetReset { get; }
        public string ClientId { get; }

        public Dictionary<string, string> Headers { get; }

        public bool IgnoreHeaders { get; }

        public KafkaConfiguration(string connectionString, string topic, string group, Dictionary<string, string> headers, string clientId, Confluent.Kafka.AutoOffsetReset autoOffsetReset = AutoOffsetReset.Earliest, bool ignoreHeaders = false)
        {
            ConnectionString = connectionString;
            Topic = topic;
            Group = group;
            Headers = headers;
            ClientId = clientId;
            AutoOffsetReset = autoOffsetReset;
            IgnoreHeaders = ignoreHeaders;
        }

        public override int GetHashCode()
        {
            var headersString = string.Concat(Headers.Select((x, y) => string.Concat(x, y)));

            return System.HashCode.Combine(ConnectionString, Topic, Group, headersString);
        }

        public string TopicFullName => string.IsNullOrEmpty(Topic) ? Group : $"{Topic}@{Group}";
    }
}