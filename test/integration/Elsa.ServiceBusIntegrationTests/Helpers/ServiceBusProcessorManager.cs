using Azure.Messaging.ServiceBus;
using Elsa.ServiceBusIntegrationTests.Contracts;
using Moq;
using Xunit.Abstractions;

namespace Elsa.ServiceBusIntegrationTests.Helpers
{
    public class ServiceBusProcessorManager : IServiceBusProcessorManager
    {
        private readonly IMock<ServiceBusClient> _serviceBusClient;
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly IDictionary<(string topicName, string subscriptionName), ServiceBusProcessorTest> _serviceBusProcessors
            = new Dictionary<(string topicName, string subscriptionName), ServiceBusProcessorTest>();
        public ServiceBusProcessorManager(IMock<ServiceBusClient> serviceBusClient, ITestOutputHelper testOutputHelper)
        {
            _serviceBusClient = serviceBusClient;
            _testOutputHelper = testOutputHelper;
        }

        public ServiceBusProcessorTest Init(string topic ,string subscription)
        {
            _serviceBusProcessors.TryGetValue((topic, subscription), out var processor);
            if (processor != null)
                throw new ArgumentException($"ServiceBusProcessor with topic:{topic}, subscription:{subscription} already initialized");

            processor = new ServiceBusProcessorTest(_testOutputHelper);
            (_serviceBusClient as Mock<ServiceBusClient>)
                .Setup(sb =>
                    sb.CreateProcessor(It.Is<string>(s => s == topic),
                    It.IsAny<string>(),
                    It.IsAny<ServiceBusProcessorOptions>())
                    )
                .Callback<string, string, ServiceBusProcessorOptions>(
                    (t, s, sbO) =>
                    {
                        _testOutputHelper.WriteLine("ServiceBusClient");
                    })
                .Returns(processor);

            _serviceBusProcessors.Add((topic,subscription), processor);
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