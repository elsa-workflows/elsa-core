using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Requests;
using JetBrains.Annotations;
using MassTransit;

namespace Elsa.MassTransit.Consumers;

[UsedImplicitly]
public class DispatchStimulusRequestConsumer(IStimulusSender stimulusSender) : IConsumer<DispatchStimulusRequest>
{
    public async Task Consume(ConsumeContext<DispatchStimulusRequest> context)
    {
        var cancellationToken = context.CancellationToken;
        var request = context.Message;
        
        if(request.ActivityTypeName != null)
        {
            await stimulusSender.SendAsync(request.ActivityTypeName, request.Stimulus!, request.Metadata, cancellationToken);
            return;
        }
        
        if(request.StimulusHash != null)
            await stimulusSender.SendAsync(request.StimulusHash!, request.Metadata, cancellationToken);
        
        throw new InvalidOperationException("Either ActivityTypeName or StimulusHash must be specified.");
    }
}