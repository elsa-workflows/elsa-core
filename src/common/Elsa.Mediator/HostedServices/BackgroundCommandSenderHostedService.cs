using System.Threading.Channels;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Command;
using Elsa.Mediator.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Mediator.HostedServices;

/// <summary>
/// Continuously reads from a channel to which commands can be sent, executing each received command.
/// </summary>
public class BackgroundCommandSenderHostedService : BackgroundService
{
    private readonly int _workerCount;
    private readonly ICommandsChannel _commandsChannel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly List<Channel<CommandContext>> _outputs;
    private readonly ILogger _logger;

    /// <inheritdoc />
    public BackgroundCommandSenderHostedService(IOptions<MediatorOptions> options, ICommandsChannel commandsChannel, IServiceScopeFactory scopeFactory, ILogger<BackgroundCommandSenderHostedService> logger)
    {
        _workerCount = options.Value.CommandWorkerCount;
        _commandsChannel = commandsChannel;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _outputs = new(_workerCount);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var index = 0;

        for (var i = 0; i < _workerCount; i++)
        {
            var output = Channel.CreateUnbounded<CommandContext>();
            _outputs.Add(output);
            _ = ReadOutputAsync(output, cancellationToken);
        }

        await foreach (var commandContext in _commandsChannel.Reader.ReadAllAsync(cancellationToken))
        {
            var output = _outputs[index];
            await output.Writer.WriteAsync(commandContext, cancellationToken);
            index = (index + 1) % _workerCount;
        }

        foreach (var output in _outputs) 
            output.Writer.Complete();
    }

    private async Task ReadOutputAsync(Channel<CommandContext> output, CancellationToken cancellationToken)
    {
        await foreach (var commandContext in output.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var commandSender = scope.ServiceProvider.GetRequiredService<ICommandSender>();

                await commandSender.SendAsync(commandContext.Command, CommandStrategy.Default, commandContext.Headers, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An unhandled exception occurred while processing the queue");
            }
        }
    }
}