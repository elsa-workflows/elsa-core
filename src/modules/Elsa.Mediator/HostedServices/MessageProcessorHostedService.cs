using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.HostedServices;

/// <summary>
/// Continuously reads from a channel to which commands can be sent, executing each received command.
/// </summary>
public class MessageProcessorHostedService<T> : BackgroundService
{
    private readonly Channel<T> _channel;
    private readonly ILogger _logger;
    private readonly IList<Channel<T>> _outputs;
    private readonly IList<MessageWorker<T>> _workers;

    public MessageProcessorHostedService(int workerCount, Channel<T> channel, ILogger<MessageProcessorHostedService<T>> logger)
    {
        _channel = channel;
        _logger = logger;
        _outputs = new List<Channel<T>>(workerCount);
        _workers = new List<MessageWorker<T>>(workerCount);
        
        for (var i = 0; i < workerCount; i++) 
            _outputs[i] = Channel.CreateUnbounded<T>();
        
        for (var i = 0; i < workerCount; i++) 
            _workers[i] = new MessageWorker<T>(Channel.CreateUnbounded<T>());
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var index = 0;
        
        await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            await _outputs[index].Writer.WriteAsync(message, cancellationToken);
            index = (index + 1) % _outputs.Count;
        }

        foreach (var output in _outputs) 
            output.Writer.Complete();
    }
}

public class MessageWorker<T>
{
    private readonly Channel<T> _channel;
    private readonly Func<T, Task> _consume;

    public MessageWorker(Channel<T> channel, Func<T, Task> consume)
    {
        _channel = channel;
        _consume = consume;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken)) 
            await _consume(message);
    }
}