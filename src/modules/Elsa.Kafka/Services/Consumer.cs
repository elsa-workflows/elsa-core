using Confluent.Kafka;

namespace Elsa.Kafka;

public class Consumer(ConsumerDefinition consumerDefinition) : IDisposable
{
    private CancellationTokenSource _cancellationTokenSource = new();
    public Func<Consumer, Message<Ignore, string>, CancellationToken, Task>? MessageReceived { get; set; }
    public ConsumerDefinition ConsumerDefinition { get; } = consumerDefinition;
    
    public void Start(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = RunAsync(_cancellationTokenSource.Token);
    }
    
    public void Stop()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }
    
    public void Dispose()
    {
        Stop();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = string.Join(",", ConsumerDefinition.BootstrapServers),
            GroupId = ConsumerDefinition.GroupId,
            AutoOffsetReset = ConsumerDefinition.AutoOffsetReset,
            EnableAutoCommit = ConsumerDefinition.EnableAutoCommit
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        
        consumer.Subscribe(ConsumerDefinition.Topics);

        while (!cancellationToken.IsCancellationRequested)
        {
            var consumeResult = consumer.Consume(cancellationToken);

            if (consumeResult.IsPartitionEOF)
                continue;

            await ProcessMessageAsync(consumeResult.Message);
        }
        
        consumer.Unsubscribe();
    }

    private Task ProcessMessageAsync(Message<Ignore, string> message)
    {
        MessageReceived?.Invoke(this, message, _cancellationTokenSource.Token);
        return Task.CompletedTask;
    }
}