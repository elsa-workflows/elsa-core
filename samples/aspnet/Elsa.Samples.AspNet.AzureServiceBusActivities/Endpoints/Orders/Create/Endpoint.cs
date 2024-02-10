using System.Net.Mime;
using Azure.Messaging.ServiceBus;
using Elsa.Common.Services;
using Elsa.Samples.AspNet.AzureServiceBusActivities.Messages;
using FastEndpoints;

namespace Elsa.Samples.AspNet.AzureServiceBusActivities.Endpoints.Orders.Create;

public class Create : EndpointWithoutRequest
{
    private readonly ServiceBusClient _client;

    public Create(ServiceBusClient client)
    {
        _client = client;
    }
    
    public override void Configure()
    {
        Post("orders");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var messageContent = new OrderCreated(
            Guid.NewGuid().ToString("N"),
            "1",
            "Pizza",
            5,
            62.5m);

        var formatter = new JsonFormatter();
        var json = await formatter.ToStringAsync(messageContent, cancellationToken);
        
        var message = new ServiceBusMessage(json)
        {
            ContentType = MediaTypeNames.Application.Json
        };
        
        await using var sender = _client.CreateSender("order-created");
        await sender.SendMessageAsync(message, cancellationToken);
    }
}