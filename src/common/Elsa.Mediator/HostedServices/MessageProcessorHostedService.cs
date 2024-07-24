using System.Threading.Channels;
using Elsa.Mediator.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.HostedServices;

/// <summary>
/// Continuously reads from a channel to which commands can be sent, executing each received command.
/// </summary>
/// <inheritdoc />
public class MessageProcessorHostedService<T>(int workerCount,
    Channel<T> channel, 
    IEnumerable<IConsumer<T>> consumers,
    ILogger<MessageProcessorHostedService<T>> logger) : BackgroundService where T : notnull
{
    private readonly int _workerCount = workerCount;
    private readonly Channel<T> _channel = channel;
    private readonly IEnumerable<IConsumer<T>> _consumers = consumers;
    private readonly ILogger _logger = logger;
    private readonly List<MessageWorker<T>> _workers = new List<MessageWorker<T>>(workerCount);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var index = 0;

        for (var i = 0; i < _workerCount; i++)
        {
            var worker = new MessageWorker<T>(Channel.CreateUnbounded<T>(), _consumers, _logger);
            _workers.Add(worker);
            _ = worker.StartAsync(cancellationToken);
        }

        await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            var worker = _workers[index];
            await worker.DeliverMessageAsync(message, cancellationToken);
            index = (index + 1) % _workerCount;
        }

        foreach (var worker in _workers)
            worker.Complete();

        _workers.Clear();
    }
}

/// <summary>
/// Represents a worker that continuously reads from a channel and processes each received message.
/// </summary>
/// <typeparam name="T">The type of message to process.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="MessageWorker{T}"/> class.
/// </remarks>
/// <param name="channel">The channel to read from.</param>
/// <param name="consumers">The consumers that will process each received message.</param>
/// <param name="logger">The logger.</param>
public class MessageWorker<T>(Channel<T> channel, IEnumerable<IConsumer<T>> consumers, ILogger logger) where T : notnull
{
    private readonly Channel<T> _channel = channel;
    private readonly IEnumerable<IConsumer<T>> _consumers = consumers;
    private readonly ILogger _logger = logger;

    /// <summary>
    /// Continuously reads from the channel and processes each received message.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken))
        foreach (var consumer in _consumers)
            await InvokeConsumerAsync(consumer, message, cancellationToken);
    }

    /// <summary>
    /// Delivers a message to the channel.
    /// </summary>
    /// <param name="message">The message to deliver.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task DeliverMessageAsync(T message, CancellationToken cancellationToken)
    {
        await _channel.Writer.WriteAsync(message, cancellationToken);
    }

    /// <summary>
    /// Completes the channel.
    /// </summary>
    public void Complete()
    {
        _channel.Writer.Complete();
    }

    private async Task InvokeConsumerAsync(IConsumer<T> consumer, T message, CancellationToken cancellationToken)
    {
        try
        {
            await consumer.ConsumeAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while invoking consumer {ConsumerType} for message {MessageType}", consumer.GetType(), message.GetType());
        }
    }
}