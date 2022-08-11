using System.Threading.Channels;
using Elsa.Mediator.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.HostedServices;

/// <summary>
/// Continuously reads from a channel to which commands can be sent, executing each received command.
/// </summary>
public class BackgroundCommandSenderHostedService : BackgroundService
{
    private readonly ChannelReader<ICommand> _channelReader;
    private readonly ICommandSender _commandSender;
    private readonly ILogger _logger;

    public BackgroundCommandSenderHostedService(ChannelReader<ICommand> channelReader, ICommandSender commandSender, ILogger<BackgroundCommandSenderHostedService> logger)
    {
        _channelReader = channelReader;
        _commandSender = commandSender;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await foreach (var request in _channelReader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await _commandSender.ExecuteAsync(request, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An unhandled exception occured while processing the queue");
            }
        }
    }
}