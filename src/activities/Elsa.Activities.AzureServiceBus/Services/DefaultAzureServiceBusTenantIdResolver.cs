// file:	Elsa.Activities.AzureServiceBus\Services\NoOpAzureServiceBusTenantIdResolver.cs
//
// summary:	Implements the no operation azure service bus tenant identifier resolver class
using Azure.Messaging.ServiceBus;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.AzureServiceBus.Services
{
    /// <summary>
    /// A no operation azure service bus tenant identifier resolver.
    /// </summary>
    public class DefaultAzureServiceBusTenantIdResolver : IAzureServiceBusTenantIdResolver
    {
        /// <summary>
        /// Resolves.
        /// </summary>
        /// <param name="message"> The message.</param>
        /// <param name="queueOrTopic"> The queue or topic.</param>
        /// <param name="subscription"> The subscription.</param>
        /// <param name="tags"> The tags.</param>
        /// <param name="cancellationToken"> A token that allows processing to be cancelled.</param>
        /// <returns>
        /// A string?
        /// </returns>
        public ValueTask<string?> ResolveAsync(ServiceBusMessage message, string queueOrTopic, string? subscription, HashSet<string> tags, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>();
        }
    }
}