using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Services;
using Elsa.Workflows.Sink.Contracts;
using Elsa.Workflows.Sink.Models;

namespace Elsa.Workflows.Sink.Implementations;

public class InProcessSinkTransport : ISinkTransport
{
    private readonly IBackgroundCommandSender _sender;

    public InProcessSinkTransport(IBackgroundCommandSender sender)
    {
        _sender = sender;
    }

    public async Task SendAsync(ExportWorkflowSinkMessage message, CancellationToken cancellationToken)
    {
        await _sender.SendAsync(message, cancellationToken);
    }
}