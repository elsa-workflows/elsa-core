using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Kafka.Activities.KafkaMessageReceived;
using Elsa.Activities.Kafka.Models;

namespace Elsa.Activities.Kafka
{
    /// <summary>
    /// A no operation kafka tenant identifier resolver.
    /// </summary>
    public class DefaultKafkaTenantIdResolver : IKafkaTenantIdResolver
    {
        /// <summary>
        /// <see cref="IKafkaTenantIdResolver"/>
        /// </returns>
        public ValueTask<string?> ResolveAsync(KafkaMessageEvent message, string topic, string? group, HashSet<string> tags, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>();
        }
    }
}