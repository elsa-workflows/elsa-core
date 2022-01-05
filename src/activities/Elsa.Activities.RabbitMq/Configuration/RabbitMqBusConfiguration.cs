using System.Collections.Generic;
using System.Linq;

namespace Elsa.Activities.RabbitMq.Configuration
{
    public class RabbitMqBusConfiguration
    {
        public string ConnectionString { get; }
        public string ExchangeName { get; }
        public string RoutingKey { get; }
        public Dictionary<string, string> Headers { get; }

        public RabbitMqBusConfiguration(string connectionString, string exchangeName, string routingKey, Dictionary<string, string> headers)
        {
            ConnectionString = connectionString;
            ExchangeName = exchangeName;
            RoutingKey = routingKey;
            Headers = headers ?? new Dictionary<string, string>();
        }

        public override int GetHashCode()
        {
            var headersString = string.Concat(Headers.Select((x, y) => string.Concat(x, y)));

            return System.HashCode.Combine(ConnectionString, ExchangeName, RoutingKey, headersString);
        }

        public string TopicFullName => string.IsNullOrEmpty(ExchangeName) ? RoutingKey : $"{RoutingKey}@{ExchangeName}";
    }
}
