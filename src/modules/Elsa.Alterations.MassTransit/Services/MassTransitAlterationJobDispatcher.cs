using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.MassTransit.Messages;
using MassTransit;

namespace Elsa.Alterations.MassTransit.Services;

/// <summary>
/// Dispatches an alteration job for execution using MassTransit.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MassTransitAlterationJobDispatcher"/> class.
/// </remarks>
public class MassTransitAlterationJobDispatcher(IBus bus) : IAlterationJobDispatcher
{
    private readonly IBus _bus = bus;

    /// <inheritdoc />
    public async ValueTask DispatchAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var message = new RunAlterationJob(jobId);
        await _bus.Send(message, cancellationToken);
    }
}