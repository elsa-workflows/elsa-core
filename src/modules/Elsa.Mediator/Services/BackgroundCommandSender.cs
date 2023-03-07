using System.Threading.Channels;
using Elsa.Mediator.Contracts;

namespace Elsa.Mediator.Services;

public class BackgroundCommandSender : IBackgroundCommandSender
{
    private readonly ChannelWriter<ICommand> _channelWriter;
    public BackgroundCommandSender(ChannelWriter<ICommand> channelWriter) => _channelWriter = channelWriter;
    public async Task SendAsync(ICommand command, CancellationToken cancellationToken = default) => await _channelWriter.WriteAsync(command, cancellationToken);
}