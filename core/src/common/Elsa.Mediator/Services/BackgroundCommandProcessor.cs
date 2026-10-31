using System.Threading.Channels;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Command;
using Elsa.Mediator.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Mediator.Services;

/// <summary>
/// Continuously reads from a channel to which commands can be sent, executing each received command.
/// </summary>
public class BackgroundCommandProcessor
{
    private readonly int _workerCount;
    private readonly ICommandsChannel _commandsChannel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundCommandProcessor"/> class.
    /// </summary>
    public BackgroundCommandProcessor(IOptions<MediatorOptions> options, ICommandsChannel commandsChannel, IServiceScopeFactory scopeFactory, ILogger<BackgroundCommandProcessor> logger)
    {
        _workerCount = options.Value.CommandWorkerCount;
        _commandsChannel = commandsChannel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Runs the processor until cancellation is requested.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var index = 0;
        var outputs = new List<Channel<CommandContext>>(_workerCount);
        var workers = new List<Task>(_workerCount);

        for (var i = 0; i < _workerCount; i++)
        {
            var output = Channel.CreateUnbounded<CommandContext>();
            outputs.Add(output);
            workers.Add(ReadOutputAsync(output, cancellationToken));
        }

        try
        {
            await foreach (var commandContext in _commandsChannel.Reader.ReadAllAsync(cancellationToken))
            {
                var output = outputs[index];
                await output.Writer.WriteAsync(commandContext, cancellationToken);
                index = (index + 1) % _workerCount;
            }
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(ex, "An operation was cancelled while processing the command queue");
        }

        foreach (var output in outputs)
            output.Writer.Complete();

        await Task.WhenAll(workers);
    }

    private async Task ReadOutputAsync(Channel<CommandContext> output, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var commandContext in output.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var commandSender = scope.ServiceProvider.GetRequiredService<ICommandSender>();
                    using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, commandContext.CancellationToken);

                    await commandSender.SendAsync(
                        commandContext.Command,
                        commandContext.CommandStrategy,
                        commandContext.Headers,
                        linkedTokenSource.Token);
                }
                catch (OperationCanceledException e)
                {
                    _logger.LogDebug(e, "An operation was cancelled while processing the command queue");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An unhandled exception occurred while processing the command queue");
                }
            }
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(ex, "An operation was cancelled while processing the command queue");
        }
    }
}
