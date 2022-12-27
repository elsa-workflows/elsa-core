using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Sink.Contracts;
using Elsa.Workflows.Sink.Models;
using MassTransit;

namespace Elsa.Workflows.Sink.Implementations;

public class MassTransitSinkTransport : ISinkTransport
{
    private readonly IBus _bus;

    public MassTransitSinkTransport(IBus bus)
    {
        _bus = bus;
    }
    
    public async Task SendAsync(ExportWorkflowSinkMessage message, CancellationToken cancellationToken)
    {
        await _bus.Publish(message, cancellationToken);
    }
}