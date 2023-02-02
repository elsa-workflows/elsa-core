// file:	Elsa.Activities.AzureServiceBus\Services\IAzureServiceBusTenantIdResolver.cs
//
// summary:	Declares the IAzureServiceBusTenantIdResolver interface
using Azure.Messaging.ServiceBus;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.AzureServiceBus.Services
{
    /// <summary>
    /// Interface for azure service bus tenant identifier resolver.
    /// </summary>
    public interface IAzureServiceBusTenantIdResolver
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
        ValueTask<string?> ResolveAsync(ServiceBusMessage message, string queueOrTopic, string? subscription, HashSet<string> tags, CancellationToken cancellationToken);
    }
}