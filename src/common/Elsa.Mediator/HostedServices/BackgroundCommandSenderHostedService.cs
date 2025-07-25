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
        _commandsChannel = commandsChannel; // The shared input channel for all commands
        _scopeFactory = scopeFactory;
        _logger = logger;
        _outputs = new(_workerCount); // Prepare a list to hold worker-specific channels
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var index = 0; // Used for round-robin distribution of work

        // Set up worker channels and start background tasks for each worker
        for (var i = 0; i < _workerCount; i++)
        {
            var output = Channel.CreateUnbounded<CommandContext>();
            _outputs.Add(output);
            // Start a background task that processes commands from this worker's channel
            _ = ReadOutputAsync(output, cancellationToken);
        }

        // Main dispatcher loop: read from the input channel and distribute to worker channels
        await foreach (var commandContext in _commandsChannel.Reader.ReadAllAsync(cancellationToken))
        {
            var output = _outputs[index];
            await output.Writer.WriteAsync(commandContext, cancellationToken);
            // Round-robin distribution - move to next worker
            index = (index + 1) % _workerCount;
        }

        // If the input channel is completed, complete all worker channels
        foreach (var output in _outputs)
            output.Writer.Complete();
    }

    private async Task ReadOutputAsync(Channel<CommandContext> output, CancellationToken cancellationToken)
    {
        // Worker task: process commands from the worker's channel
        await foreach (var commandContext in output.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                // Create a fresh scope for each command to ensure proper service lifetime
                using var scope = _scopeFactory.CreateScope();
                var commandSender = scope.ServiceProvider.GetRequiredService<ICommandSender>();

                // Link the service cancellation token with the command's token to ensure proper cancellation
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    commandContext.CancellationToken);

                // Process the command using the command sender service with the linked token
                await commandSender.SendAsync(
                    commandContext.Command,
                    CommandStrategy.Default,
                    commandContext.Headers,
                    linkedTokenSource.Token);
            }
            catch (Exception e)
            {
                // Log errors but continue processing other commands
                _logger.LogError(e, "An unhandled exception occured while processing the queue");
            }
        }
    }
}