using Elsa.AzureServiceBus.Models;

namespace Elsa.AzureServiceBus.Services;

/// <summary>
/// Provides subscription definitions to the system. 
/// </summary>
public interface ISubscriptionProvider
{
    ValueTask<ICollection<SubscriptionDefinition>> GetSubscriptionsAsync(CancellationToken cancellationToken);
}