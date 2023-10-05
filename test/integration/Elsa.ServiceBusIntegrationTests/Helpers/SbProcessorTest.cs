using Azure.Messaging.ServiceBus;
using Elsa.ServiceBusIntegrationTests.Contracts;
using System.Text.Json;
using Xunit.Abstractions;

namespace Elsa.ServiceBusIntegrationTests.Helpers
{

    public class ServiceBusProcessorManager : IServiceBusProcessorManager
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly IDictionary<(string topicName, string subscriptionName), ServiceBusProcessorTest> _serviceBusProcessors
            = new Dictionary<(string topicName, string subscriptionName), ServiceBusProcessorTest>();
        public ServiceBusProcessorManager(ServiceBusClient serviceBusClient)
        {
            _serviceBusClient = serviceBusClient;
        }

        public ServiceBusProcessorTest Init(string topic ,string subscription)
        {
            _serviceBusProcessors.TryGetValue((topic, subscription), out var processor);
            if (processor != null)
                throw new ArgumentException($"ServiceBusProcessor with topic:{topic}, subscription:{subscription} already initialized");

            var sbProcessor = new ServiceBusProcessorTest(false,);

        }
        public ServiceBusProcessorTest Get(string topic, string subscription)
        {
            throw new NotImplementedException();
        }
    }
    public class ServiceBusProcessorTest : ServiceBusProcessor
    {
        private readonly List<string> _bookmarkIds;
        private readonly ITestOutputHelper _testOutputHelper;

        public bool _send { get; set; }

        public ServiceBusProcessorTest(bool send, List<string> bookmarkIds, ITestOutputHelper testOutputHelper)
        {
            _send = send;
            _bookmarkIds = bookmarkIds;
            _testOutputHelper = testOutputHelper;
        }
        public async Task SendMessage<T>(T payload, string correlationId, int attempt = 1)
        {
            var args = CreateMessageArgs(payload, correlationId, attempt);
            await base.OnProcessMessageAsync(args);
        }

        public ProcessMessageEventArgs CreateMessageArgs<T>(T payload, string correlationId, int deliveryCount = 1)
        {
            var payloadJson = JsonSerializer.Serialize(payload);
            var props = new Dictionary<string, object>() { };
         
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: BinaryData.FromString(payloadJson),
                deliveryCount: deliveryCount,
                correlationId: correlationId,
                properties: props
                );
            

            var args = new ProcessMessageEventArgs(message, null, new CancellationToken());

            return args;
        }
        public override async Task StartProcessingAsync(CancellationToken cancellationToken = default)
        {
            _testOutputHelper.WriteLine("Receiving Service Bus Message");
            if (_send)
            {
                foreach (var b in _bookmarkIds)
                {
                    _testOutputHelper.WriteLine($"Simulating Sb Message with bookmark {b}");
                    await SendMessage(new { Hello = "World", bookmark = b }, b);
                    await Task.Delay(TimeSpan.FromSeconds(3));
                }


            }


            _send = true;
        }

    }
}