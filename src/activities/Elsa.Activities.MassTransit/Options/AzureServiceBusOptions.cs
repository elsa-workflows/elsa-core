using Microsoft.Extensions.Hosting;

namespace Elsa.Activities.MassTransit.Options
{
    public class AzureServiceBusOptions
    {
        public string Host { get; set; }
        public string ConnectionString { get; set; }
    }
}