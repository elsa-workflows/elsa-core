using System.Threading.Channels;
using Elsa.Mediator.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.HostedServices;

/// <summary>
/// Continuously reads from a channel to which commands can be sent, executing each received command.
/// </summary>
/// <inheritdoc />
public class BackgroundCommandSenderHostedService(int workerCount, 
    ICommandsChannel commandsChannel,
    IServiceScopeFactory scopeFactory,
    ILogger<BackgroundCommandSenderHostedService> logger) : BackgroundService
{
    private readonly int _workerCount = workerCount;
    private readonly ICommandsChannel _commandsChannel = commandsChannel;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly List<Channel<ICommand>> _outputs = new List<Channel<ICommand>>(workerCount);
    private readonly ILogger _logger = logger;

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

        await foreach (var command in _commandsChannel.Reader.ReadAllAsync(cancellationToken))
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
                using var scope = _scopeFactory.CreateScope();
                var commandSender = scope.ServiceProvider.GetRequiredService<ICommandSender>();

                await commandSender.SendAsync(command, CommandStrategy.Default, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An unhandled exception occured while processing the queue");
            }
        }
    }
}