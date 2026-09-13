using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Streaming;
using System.Threading.Tasks;

namespace Elsa.AI.Host.UnitTests.Streaming;

public class AIStreamEventMapperTests
{
    [Test]
    [DisplayName("Provider events map to Elsa stream events")]
    public async Task ProviderEventsMapToElsaStreamEvents()
    {
        var mapper = new AIStreamEventMapper();

        var mapped = mapper.Map("conversation-1", new AIProviderEvent
        {
            Type = "assistant.delta",
            Sequence = 7,
            Timestamp = DateTimeOffset.UtcNow
        });

        await Assert.That(mapped.Type).IsEqualTo("assistant.delta");
        await Assert.That(mapped.ConversationId).IsEqualTo("conversation-1");
        await Assert.That(mapped.Sequence).IsEqualTo(7);
    }
}