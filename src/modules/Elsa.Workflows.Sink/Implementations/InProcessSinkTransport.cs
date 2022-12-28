using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Services;
using Elsa.Workflows.Sink.Contracts;
using Elsa.Workflows.Sink.Models;

namespace Elsa.Workflows.Sink.Implementations;

public class InProcessSinkTransport : ISinkTransport
{
    private readonly IBackgroundEventPublisher _publisher;

    public InProcessSinkTransport(IBackgroundEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task SendAsync(ExportWorkflowSinkMessage message, CancellationToken cancellationToken)
    {
        await _publisher.PublishAsync(message, cancellationToken);
    }
}