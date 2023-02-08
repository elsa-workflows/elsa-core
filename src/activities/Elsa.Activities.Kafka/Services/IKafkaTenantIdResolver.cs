using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Kafka.Models;

namespace Elsa.Activities.Kafka
{
    /// <summary>
    /// Interface for kafka tenant identifier resolver.
    /// </summary>
    public interface IKafkaTenantIdResolver
    {
        /// <summary>
        /// Resolves.
        /// </summary>
        /// <param name="message"> The message.</param>
        /// <param name="topic"> The topic.</param>
        /// <param name="group"> The consumer group.</param>
        /// <param name="tags"> The tags.</param>
        /// <param name="cancellationToken"> A token that allows processing to be cancelled.</param>
        /// <returns>
        /// A string?
        /// </returns>
        ValueTask<string?> ResolveAsync(KafkaMessageEvent message, string topic, string? group, HashSet<string> tags, CancellationToken cancellationToken);
    }
}