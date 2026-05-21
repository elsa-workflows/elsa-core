using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Endpoints.Ai.Capabilities;
using Elsa.AI.Host.Options;
using Microsoft.Extensions.Options;

namespace Elsa.AI.IntegrationTests;

public class AiCapabilitiesEndpointTests
{
    [Fact(DisplayName = "Capabilities endpoint advertises Weaver MVP capabilities")]
    public async Task CapabilitiesEndpointAdvertisesWeaverMvpCapabilities()
    {
        var endpoint = new Endpoint(
            Options.Create(new AiHostOptions()),
            [new TestAiProvider()],
            [new TestConversationStore()],
            [new TestProposalStore()]);

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        Assert.True(response.Streaming);
        Assert.True(response.ConversationPersistence);
        Assert.True(response.ProposalReview);
        Assert.Contains("WorkflowDefinition", response.SupportedAttachmentKinds);
        Assert.Contains("WorkflowInstance", response.SupportedAttachmentKinds);
        Assert.DoesNotContain("ActivitySelection", response.SupportedAttachmentKinds);
        Assert.DoesNotContain("DiagnosticsScope", response.SupportedAttachmentKinds);
        Assert.DoesNotContain("TimeRange", response.SupportedAttachmentKinds);
    }

    [Fact(DisplayName = "Capabilities endpoint hides unavailable capabilities")]
    public async Task CapabilitiesEndpointHidesUnavailableCapabilities()
    {
        var endpoint = new Endpoint(
            Options.Create(new AiHostOptions { StreamingEnabled = false }),
            [new TestAiProvider()],
            [new TestConversationStore()],
            []);

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        Assert.False(response.Streaming);
        Assert.True(response.ConversationPersistence);
        Assert.False(response.ProposalReview);
    }

    private class TestAiProvider : IAiProvider
    {
        public string Name => "test";

        public ValueTask<AiSessionHandle> CreateSessionAsync(CreateAiSessionRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AiSessionHandle { Id = request.ConversationId });

        public async IAsyncEnumerable<AiProviderEvent> ExecuteTurnAsync(AiTurnRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield break;
        }
    }

    private class TestConversationStore : IAiConversationStore
    {
        public ValueTask<AiConversation?> FindAsync(string id, CancellationToken cancellationToken = default) => ValueTask.FromResult<AiConversation?>(null);
        public ValueTask SaveAsync(AiConversation conversation, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    private class TestProposalStore : IAiProposalStore
    {
        public ValueTask<AiProposal?> FindAsync(string id, string? tenantId, CancellationToken cancellationToken = default) => ValueTask.FromResult<AiProposal?>(null);
        public ValueTask SaveAsync(AiProposal proposal, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
