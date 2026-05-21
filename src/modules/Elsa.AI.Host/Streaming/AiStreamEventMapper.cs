using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Host.Streaming;

public class AiStreamEventMapper
{
    public AiStreamEvent Map(string conversationId, AiProviderEvent providerEvent) =>
        new()
        {
            Type = providerEvent.Type,
            ConversationId = conversationId,
            Sequence = providerEvent.Sequence,
            Timestamp = providerEvent.Timestamp,
            Data = providerEvent.Data
        };
}
