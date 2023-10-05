using Elsa.ServiceBusIntegrationTests.Helpers;

namespace Elsa.ServiceBusIntegrationTests.Contracts
{
    public interface IServiceBusProcessorManager
    {
        public ServiceBusProcessorTest Get(string topic, string subscription);
        public ServiceBusProcessorTest Init(string topic, string subscription);
    }
}