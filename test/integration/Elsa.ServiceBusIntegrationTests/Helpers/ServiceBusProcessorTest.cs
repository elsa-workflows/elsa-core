using Azure.Messaging.ServiceBus;
using System.Text.Json;
using Xunit.Abstractions;

namespace Elsa.ServiceBusIntegrationTests.Helpers
{
    public class ServiceBusProcessorTest : ServiceBusProcessor
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ServiceBusProcessorTest(ITestOutputHelper testOutputHelper)
        {
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
        }

    }
}