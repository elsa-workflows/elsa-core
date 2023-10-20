using Elsa.ServiceBus.IntegrationTests.Helpers;

namespace Elsa.ServiceBus.IntegrationTests.Contracts
{
    public interface IServiceBusProcessorManager
    {
        public ServiceBusProcessorTest Get(string topic, string subscription);
        public ServiceBusProcessorTest Init(string topic, string subscription);
    }
}