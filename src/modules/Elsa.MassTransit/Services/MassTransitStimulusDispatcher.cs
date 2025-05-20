using Elsa.MassTransit.Messages;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;
using MassTransit;

namespace Elsa.MassTransit.Services;

public class MassTransitStimulusDispatcher(IBus bus, IPayloadSerializer payloadSerializer) : IStimulusDispatcher
{
    public async Task<DispatchStimulusResponse> SendAsync(DispatchStimulusRequest request, CancellationToken cancellationToken = default)
    {
        var json = payloadSerializer.Serialize(request);
        var message = new DispatchStimulus(json);
        await bus.Publish(message, cancellationToken);
        return DispatchStimulusResponse.Empty;
    }
}