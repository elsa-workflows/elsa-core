using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// A simple implementation that queues the specified request for delivering stimuli on a non-durable background worker.
/// </summary>
public class BackgroundStimulusDispatcher(ICommandSender commandSender) : IStimulusDispatcher
{
    /// <inheritdoc />
    public async Task<DispatchStimulusResponse> SendAsync(DispatchStimulusRequest request, CancellationToken cancellationToken = default)
    {
        var command = new DispatchStimulusCommand(request);
        await commandSender.SendAsync(command, CommandStrategy.Background, cancellationToken);
        return DispatchStimulusResponse.Empty;
    }
}