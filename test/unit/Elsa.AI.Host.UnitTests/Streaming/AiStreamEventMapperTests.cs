using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Streaming;

namespace Elsa.AI.Host.UnitTests.Streaming;

public class AiStreamEventMapperTests
{
    [Fact(DisplayName = "Provider events map to Elsa stream events")]
    public void ProviderEventsMapToElsaStreamEvents()
    {
        var mapper = new AiStreamEventMapper();

        var mapped = mapper.Map("conversation-1", new AiProviderEvent
        {
            Type = "assistant.delta",
            Sequence = 7,
            Timestamp = DateTimeOffset.UtcNow
        });

        Assert.Equal("assistant.delta", mapped.Type);
        Assert.Equal("conversation-1", mapped.ConversationId);
        Assert.Equal(7, mapped.Sequence);
    }
}
