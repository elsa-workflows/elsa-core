using Azure.Messaging.ServiceBus;
using Elsa.Workflows.ComponentTests.Scenarios.AzureServiceBus.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.AzureServiceBus;

public class AzureServiceBusTests(App app) : AppComponentTest(app)
{
    [Fact]
    public async Task WorkflowReceivesMessage_WhenSendingMessageToTopic()
    {
        var client = Scope.ServiceProvider.GetRequiredService<ServiceBusClient>();
        var topic = ReceiveMessageWorkflow.Topic;

        await using var sender = client.CreateSender(topic);
        
        var message = new ServiceBusMessage("Hello World");
        await sender.SendMessageAsync(message);
    }
}