using Elsa.AzureServiceBus.Models;

namespace Elsa.AzureServiceBus.Contracts;

/// <summary>
/// Provides subscription definitions to the system. 
/// </summary>
[Obsolete("Use AzureServiceBusOptions.Topics instead.")]

public interface ISubscriptionProvider
{
    /// <summary>
    /// Return a list of <see cref="SubscriptionDefinition"/>s.
    /// </summary>
    ValueTask<ICollection<SubscriptionDefinition>> GetSubscriptionsAsync(CancellationToken cancellationToken);
}