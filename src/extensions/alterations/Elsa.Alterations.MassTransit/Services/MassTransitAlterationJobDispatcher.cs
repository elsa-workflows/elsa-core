using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.MassTransit.Messages;
using MassTransit;

namespace Elsa.Alterations.MassTransit.Services;

/// <summary>
/// Dispatches an alteration job for execution using MassTransit.
/// </summary>
public class MassTransitAlterationJobDispatcher : IAlterationJobDispatcher
{
    private readonly IBus _bus;

    /// <summary>
    /// Initializes a new instance of the <see cref="MassTransitAlterationJobDispatcher"/> class.
    /// </summary>
    public MassTransitAlterationJobDispatcher(IBus bus)
    {
        _bus = bus;
    }
    
    /// <inheritdoc />
    public async ValueTask DispatchAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var message = new RunAlterationJob(jobId);
        await _bus.Send(message, cancellationToken);
    }
}