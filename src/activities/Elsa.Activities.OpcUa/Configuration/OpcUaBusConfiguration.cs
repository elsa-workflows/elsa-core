using System.Collections.Generic;
using System.Linq;

namespace Elsa.Activities.OpcUa.Configuration
{
    public class OpcUaBusConfiguration
    {
        public string ConnectionString { get; }
        public int? PublishingInterval { get; } 
        public int? SessionTimeout { get; } 
        public int? OperationTimeout { get; } 
        public string ClientId { get; }
        public Dictionary<string, string> Tags { get; }

        public OpcUaBusConfiguration(string connectionString, string clientId, Dictionary<string, string> tags, 
                                     int publishingInterval = 1000, int sessionTimeout = 60000, int operationTimeout = 15000)
        {
            ConnectionString = connectionString;
            ClientId = clientId;
            PublishingInterval = publishingInterval;
            SessionTimeout = sessionTimeout;
            OperationTimeout = operationTimeout;
            Tags = tags ?? new Dictionary<string, string>();
        }

        public override int GetHashCode()
        {
            var TagsString = string.Concat(Tags.Select((x, y) => string.Concat(x, y)));
            var hash = System.HashCode.Combine(ConnectionString,PublishingInterval,SessionTimeout,OperationTimeout, TagsString);
            return hash;
        }

    }
}
