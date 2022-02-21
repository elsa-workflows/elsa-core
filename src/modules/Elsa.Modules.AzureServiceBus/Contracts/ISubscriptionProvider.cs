using Elsa.Modules.AzureServiceBus.Models;

namespace Elsa.Modules.AzureServiceBus.Contracts;

/// <summary>
/// Provides subscription definitions to the system. 
/// </summary>
public interface ISubscriptionProvider
{
    ValueTask<ICollection<SubscriptionDefinition>> GetSubscriptionsAsync(CancellationToken cancellationToken);
}