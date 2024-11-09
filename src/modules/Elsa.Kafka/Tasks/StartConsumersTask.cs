using System.Runtime.CompilerServices;
using Confluent.Kafka;
using Elsa.Common;
using Elsa.Kafka.Notifications;
using Elsa.Mediator.Contracts;
using JetBrains.Annotations;
using Open.Linq.AsyncExtensions;

namespace Elsa.Kafka;

[UsedImplicitly]
public class StartConsumersStartupTask(IEnumerable<IConsumerDefinitionProvider> providers, IMediator mediator) : BackgroundTask
{
    private readonly ICollection<Consumer> _consumers = new List<Consumer>();

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await CreateConsumersAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in _consumers)
            consumer.Stop();

        _consumers.Clear();
        return Task.CompletedTask;
    }
    
    private async Task CreateConsumersAsync(CancellationToken cancellationToken)
    {
        var consumerDefinitions = await GetConsumerDefinitionsAsync(cancellationToken).ToListAsync(cancellationToken);
        
        foreach (var consumerDefinition in consumerDefinitions)
        {
            var consumer = new Consumer(consumerDefinition);
            _consumers.Add(consumer);
            consumer.MessageReceived = OnMessageReceivedAsync;
            consumer.Start(CancellationToken.None);
        }
    }

    private async Task OnMessageReceivedAsync(Consumer consumer, Message<Ignore, string> arg, CancellationToken cancellationToken)
    {
        var notification = new TransportMessageReceived(consumer, new KafkaTransportMessage(arg.Key, arg.Value, arg.Headers, arg.Timestamp));
        await mediator.SendAsync(notification, cancellationToken);
    }

    private async IAsyncEnumerable<ConsumerDefinition> GetConsumerDefinitionsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var provider in providers)
        {
            var consumerDefinitions = await provider.GetConsumerConfigsAsync(cancellationToken).ToList();

            foreach (var consumerDefinition in consumerDefinitions)
                yield return consumerDefinition;
        }
    }
}