using System.Threading.Channels;
using Elsa.Mediator.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Mediator.Implementations;

public class DefaultMessageSender : IMessageSender
{
    private readonly IServiceProvider _serviceProvider;

    public DefaultMessageSender(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public async Task SendAsync<T>(T message, CancellationToken cancellationToken)
    {
        var channel = _serviceProvider.GetRequiredService<Channel<T>>();
        await channel.Writer.WriteAsync(message, cancellationToken);
    }
}