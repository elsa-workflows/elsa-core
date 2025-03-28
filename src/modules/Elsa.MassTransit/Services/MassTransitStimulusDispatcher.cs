using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;
using MassTransit;

namespace Elsa.MassTransit.Services;

public class MassTransitStimulusDispatcher(IBus bus) : IStimulusDispatcher
{
    public async Task<DispatchStimulusResponse> SendAsync(DispatchStimulusRequest request, CancellationToken cancellationToken = default)
    {
        await bus.Publish(request, cancellationToken);
        return DispatchStimulusResponse.Empty;
    }
}