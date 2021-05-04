namespace Elsa.Activities.AzureServiceBus.Options
{
    public class AzureServiceBusOptions
    {
        public string ConnectionString { get; set; } = default!;
        public int MaxConcurrentCalls { get; set; } = 1;
    }
}