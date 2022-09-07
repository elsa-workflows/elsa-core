using System.Threading.Channels;
using Elsa.Mediator.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.HostedServices;

/// <summary>
/// Continuously reads from a channel to which commands can be sent, executing each received command.
/// </summary>
public class MessageProcessorHostedService<T> : BackgroundService
{
    private readonly Channel<T> _channel;
    private readonly IConsumer<T> _consumer;
    private readonly ILogger _logger;
    private readonly IList<MessageWorker<T>> _workers;

    public MessageProcessorHostedService(int workerCount, Channel<T> channel, IConsumer<T> consumer, ILogger<MessageProcessorHostedService<T>> logger)
    {
        _channel = channel;
        _consumer = consumer;
        _logger = logger;
        _workers = new List<MessageWorker<T>>(workerCount);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var index = 0;
        
        for (var i = 0; i < _workers.Count; i++)
        {
            var worker = new MessageWorker<T>(Channel.CreateUnbounded<T>(), _consumer);
            _workers[i] = worker;
            _ = worker.StartAsync(cancellationToken);
        }
        
        await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            var worker = _workers[index];
            await worker.DeliverMessageAsync(message, cancellationToken);
            index = (index + 1) % _workers.Count;
        }
   
        foreach (var worker in _workers) 
            worker.Complete();
        
        _workers.Clear();
    }
}

public class MessageWorker<T>
{
    private readonly Channel<T> _channel;
    private readonly IConsumer<T> _consumer;

    public MessageWorker(Channel<T> channel, IConsumer<T> consumer)
    {
        _channel = channel;
        _consumer = consumer;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken)) 
            await _consumer.ConsumeAsync(message, cancellationToken);
    }

    public async Task DeliverMessageAsync(T message, CancellationToken cancellationToken)
    {
        await _channel.Writer.WriteAsync(message, cancellationToken);
    }

    public void Complete()
    {
        _channel.Writer.Complete();
    }
}