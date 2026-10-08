using Elsa.AI.Abstractions.Models;
using System.Text.Json.Nodes;

namespace Elsa.AI.Host.Streaming;

public class AIStreamEventMapper
{
    public AIStreamEvent Map(string conversationId, AIProviderEvent providerEvent) =>
        new()
        {
            Type = providerEvent.Type,
            ConversationId = conversationId,
            Sequence = providerEvent.Sequence,
            Timestamp = providerEvent.Timestamp,
            Data = (JsonObject)providerEvent.Data.DeepClone()
        };
}
