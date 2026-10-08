namespace Elsa.ServiceBus.MassTransit.AzureServiceBus.Options;

public class SubscriptionCleanupOptions
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromDays(7);
}