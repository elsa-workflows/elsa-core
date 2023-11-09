using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;

namespace Elsa.Activities.RabbitMq.Configuration
{
    public class RabbitMqBusConfiguration
    {
        public string ConnectionString { get; }
        public string ExchangeName { get; }
        public string RoutingKey { get; }
        public Dictionary<string, string> Headers { get; }
        public string ClientId { get; }
        public bool AutoDeleteQueue { get; }
        public bool EnableSsl { get; }
        public string SslHost { get; }
        public SslProtocols SslProtocols { get; }

        public RabbitMqBusConfiguration(string connectionString, string exchangeName, string routingKey, Dictionary<string, string> headers, string clientId, bool enableSsl, string sslHost, IEnumerable<string> sslProtocols, bool autoDeleteQueue = false)
        {
            ConnectionString = connectionString;
            ExchangeName = exchangeName;
            RoutingKey = routingKey;
            Headers = headers ?? new Dictionary<string, string>();
            EnableSsl = enableSsl;
            SslHost = sslHost;
            SslProtocols = ResolveSslProtocols(sslProtocols ?? Array.Empty<string>());
            ClientId = clientId;
            AutoDeleteQueue = autoDeleteQueue;
        }

        public override int GetHashCode()
        {
            var headersString = string.Concat(Headers.Select((x, y) => string.Concat(x, y)));

            return System.HashCode.Combine(ConnectionString, ExchangeName, RoutingKey, headersString);
        }

        public string TopicFullName => string.IsNullOrEmpty(ExchangeName) ? RoutingKey : $"{RoutingKey}@{ExchangeName}";

        public IEnumerable<string> SslProtocolsString => Enum.GetValues(typeof(SslProtocols))
            .Cast<SslProtocols>()
            .Where(c => SslProtocols.HasFlag(c) && c != SslProtocols.None)
            .Select(c => c.ToString());

        private SslProtocols ResolveSslProtocols(IEnumerable<string> sslProtocols)
        {
            var parsed = sslProtocols
                .Select(s =>
                {
                    var val = (SslProtocols)Enum.Parse(typeof(System.Security.Authentication.SslProtocols), s);
                    return val;
                }).ToList();

            SslProtocols values = SslProtocols.None;

            foreach (var sslProtocol in parsed)
            {
                values |= sslProtocol;
            }
            return values;
        }
    }
}
