using Elsa.ServiceBusIntegrationTests.Helpers;

namespace Elsa.ServiceBusIntegrationTests.Contracts
{
    public interface IServiceBusProcessorManager
    {
        public ServiceBusProcessorTest Get(string topic, string subscription);
        ServiceBusProcessorTest Init(string topic, string subscription);
    }
}