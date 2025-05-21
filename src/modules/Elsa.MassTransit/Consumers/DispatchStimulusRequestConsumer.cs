using Elsa.MassTransit.Messages;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Requests;
using JetBrains.Annotations;
using MassTransit;

namespace Elsa.MassTransit.Consumers;

[UsedImplicitly]
public class DispatchStimulusRequestConsumer(IStimulusSender stimulusSender, IPayloadSerializer payloadSerializer) : IConsumer<DispatchStimulus>
{
    public async Task Consume(ConsumeContext<DispatchStimulus> context)
    {
        var cancellationToken = context.CancellationToken;
        var message = context.Message;
        var json = message.SerializedRequest;
        var request = payloadSerializer.Deserialize<DispatchStimulusRequest>(json);

        if (request.ActivityTypeName != null)
        {
            await stimulusSender.SendAsync(request.ActivityTypeName, request.Stimulus!, request.Metadata, cancellationToken);
            return;
        }

        if (request.StimulusHash != null)
        {
            await stimulusSender.SendAsync(request.StimulusHash!, request.Metadata, cancellationToken);
            return;
        }

        throw new InvalidOperationException("Either ActivityTypeName or StimulusHash must be specified.");
    }
}