using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Endpoints.AI.Capabilities;
using Elsa.AI.Host.Options;
using Microsoft.Extensions.DependencyInjection;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;
using System.Threading.Tasks;

namespace Elsa.AI.Host.UnitTests.Grounding;

public class AIGroundingCapabilityTests
{
    [Test]
    [DisplayName("Capability descriptor reports disabled grounding family reason")]
    public async Task CapabilityDescriptorReportsDisabledGroundingFamilyReason()
    {
        using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var options = new AIHostOptions();
        options.Grounding.ActivityGroundingEnabled = false;
        var endpoint = new Endpoint(
            MicrosoftOptions.Create(options),
            [],
            [new TestConversationStore()],
            [],
            serviceProvider.GetRequiredService<IServiceScopeFactory>());

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        var activities = await Assert.That(response.Grounding).HasSingleItem(x => x.Family == "activities");
        await Assert.That(activities.Available).IsFalse();
        await Assert.That(activities.DisabledReasons).Contains("Grounding family is disabled by configuration.");
    }

    private class TestConversationStore : IAIConversationStore
    {
        public ValueTask<AIConversation?> FindAsync(string id, CancellationToken cancellationToken = default) => ValueTask.FromResult<AIConversation?>(null);
        public ValueTask SaveAsync(AIConversation conversation, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
