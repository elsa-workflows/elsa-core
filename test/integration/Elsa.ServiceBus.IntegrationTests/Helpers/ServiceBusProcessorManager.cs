using Azure.Messaging.ServiceBus;
using Elsa.ServiceBus.IntegrationTests.Contracts;
using NSubstitute;
using Xunit.Abstractions;

namespace Elsa.ServiceBus.IntegrationTests.Helpers
{
    public class ServiceBusProcessorManager : IServiceBusProcessorManager
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly IDictionary<(string topicName, string subscriptionName), ServiceBusProcessorTest> _serviceBusProcessors = new Dictionary<(string topicName, string subscriptionName), ServiceBusProcessorTest>();

        public ServiceBusProcessorManager(ServiceBusClient serviceBusClient, ITestOutputHelper testOutputHelper)
        {
            _serviceBusClient = serviceBusClient;
            _testOutputHelper = testOutputHelper;
        }

        public ServiceBusProcessorTest Init(string topic, string subscription)
        {
            _serviceBusProcessors.TryGetValue((topic, subscription), out var processor);
            if (processor != null)
                throw new ArgumentException($"ServiceBusProcessor with topic:{topic}, subscription:{subscription} already initialized");

            processor = new ServiceBusProcessorTest(_testOutputHelper);
            _serviceBusClient
                .CreateProcessor(Arg.Is<string>(s => s == topic),
                    Arg.Any<string>(),
                    Arg.Any<ServiceBusProcessorOptions>()
                )
                .Returns((c) =>
                {
                    _testOutputHelper.WriteLine("ServiceBusClient");
                    return processor;
                });

            _serviceBusProcessors.Add((topic, subscription), processor);
            return processor;
        }

        public ServiceBusProcessorTest Get(string topic, string subscription)
        {
            _serviceBusProcessors.TryGetValue((topic, subscription), out var processor);
            if (processor == null)
                throw new ArgumentException($"ServiceBusProcessor with topic:{topic}, subscription:{subscription} not exist");

            return processor;
        }
    }
}