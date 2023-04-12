using System.Threading.Channels;
using Elsa.Mediator.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.HostedServices;

/// <summary>
/// Continuously reads from a channel to which commands can be sent, executing each received command.
/// </summary>
public class BackgroundCommandSenderHostedService : BackgroundService
{
    private readonly int _workerCount;
    private readonly ChannelReader<ICommand> _channelReader;
    private readonly ICommandSender _commandSender;
    private readonly IList<Channel<ICommand>> _outputs;
    private readonly ILogger _logger;

    /// <inheritdoc />
    public BackgroundCommandSenderHostedService(int workerCount, ChannelReader<ICommand> channelReader, ICommandSender commandSender, ILogger<BackgroundCommandSenderHostedService> logger)
    {
        _workerCount = workerCount;
        _channelReader = channelReader;
        _commandSender = commandSender;
        _logger = logger;
        _outputs = new List<Channel<ICommand>>(workerCount);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var index = 0;

        for (var i = 0; i < _workerCount; i++)
        {
            var output = Channel.CreateUnbounded<ICommand>();
            _outputs.Add(output);
            _ = ReadOutputAsync(output, cancellationToken);
        }

        await foreach (var command in _channelReader.ReadAllAsync(cancellationToken))
        {
            var output = _outputs[index];
            await output.Writer.WriteAsync(command, cancellationToken);
            index = (index + 1) % _workerCount;
        }

        foreach (var output in _outputs)
        {
            output.Writer.Complete();
        }
    }

    private async Task ReadOutputAsync(Channel<ICommand> output, CancellationToken cancellationToken)
    {
        await foreach (var command in output.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await _commandSender.SendAsync(command, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An unhandled exception occured while processing the queue");
            }
        }
    }
}