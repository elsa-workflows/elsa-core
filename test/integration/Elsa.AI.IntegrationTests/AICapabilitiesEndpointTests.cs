using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Endpoints.AI.Capabilities;
using Elsa.AI.Host.Options;
using Elsa.AI.Host.Services;
using Microsoft.Extensions.DependencyInjection;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace Elsa.AI.IntegrationTests;

public class AICapabilitiesEndpointTests
{
    [Fact(DisplayName = "Capabilities endpoint advertises Weaver MVP capabilities")]
    public async Task CapabilitiesEndpointAdvertisesWeaverMvpCapabilities()
    {
        var endpoint = new Endpoint(
            MicrosoftOptions.Create(new AIHostOptions { ConversationPersistenceEnabled = true }),
            [new TestAIProvider()],
            [new TestConversationStore()],
            [new TestProposalStore()],
            CreateScopeFactory());

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        Assert.True(response.Streaming);
        Assert.True(response.ConversationPersistence);
        Assert.True(response.ProposalReview);
        Assert.Contains("WorkflowDefinition", response.SupportedAttachmentKinds);
        Assert.Contains("WorkflowInstance", response.SupportedAttachmentKinds);
        Assert.Contains("Activity", response.SupportedAttachmentKinds);
        Assert.Contains("DiagnosticsScope", response.SupportedAttachmentKinds);
        Assert.Contains("TimeRange", response.SupportedAttachmentKinds);
        Assert.Contains(response.Grounding, x => x.Family == "activities");
        Assert.Contains(response.Grounding, x => x.Family == "workflows");
        Assert.Contains(response.Grounding, x => x.Family == "proposals");
        Assert.Contains(response.Grounding, x => x.Family == "runtime");
    }

    [Fact(DisplayName = "Capabilities endpoint hides unavailable capabilities")]
    public async Task CapabilitiesEndpointHidesUnavailableCapabilities()
    {
        var endpoint = new Endpoint(
            MicrosoftOptions.Create(new AIHostOptions { StreamingEnabled = false, ConversationPersistenceEnabled = true }),
            [new TestAIProvider()],
            [new TestConversationStore()],
            [],
            CreateScopeFactory());

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        Assert.False(response.Streaming);
        Assert.True(response.ConversationPersistence);
        Assert.False(response.ProposalReview);
    }

    [Fact(DisplayName = "Capabilities endpoint hides streaming when multiple providers need a default")]
    public async Task CapabilitiesEndpointHidesStreamingWhenMultipleProvidersNeedADefault()
    {
        var endpoint = new Endpoint(
            MicrosoftOptions.Create(new AIHostOptions()),
            [new TestAIProvider("provider-1"), new TestAIProvider("provider-2")],
            [new TestConversationStore()],
            [],
            CreateScopeFactory());

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        Assert.False(response.Streaming);
    }

    [Fact(DisplayName = "Capabilities endpoint advertises streaming when configured default provider resolves")]
    public async Task CapabilitiesEndpointAdvertisesStreamingWhenConfiguredDefaultProviderResolves()
    {
        var endpoint = new Endpoint(
            MicrosoftOptions.Create(new AIHostOptions { DefaultProviderName = "provider-2" }),
            [new TestAIProvider("provider-1"), new TestAIProvider("provider-2")],
            [new TestConversationStore()],
            [],
            CreateScopeFactory());

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        Assert.True(response.Streaming);
    }

    [Fact(DisplayName = "Capabilities endpoint advertises registered durable conversation persistence")]
    public async Task CapabilitiesEndpointAdvertisesRegisteredDurableConversationPersistence()
    {
        var endpoint = new Endpoint(
            MicrosoftOptions.Create(new AIHostOptions()),
            [new TestAIProvider()],
            [new TestConversationStore()],
            [],
            CreateScopeFactory());

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        Assert.True(response.ConversationPersistence);
    }

    [Fact(DisplayName = "Capabilities endpoint does not advertise in-memory conversation persistence")]
    public async Task CapabilitiesEndpointDoesNotAdvertiseInMemoryConversationPersistence()
    {
        var endpoint = new Endpoint(
            MicrosoftOptions.Create(new AIHostOptions { ConversationPersistenceEnabled = true }),
            [new TestAIProvider()],
            [new InMemoryAIConversationStore()],
            [new TestProposalStore()],
            CreateScopeFactory());

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        Assert.False(response.ConversationPersistence);
    }

    [Fact(DisplayName = "Capabilities endpoint does not advertise disabled conversation persistence")]
    public async Task CapabilitiesEndpointDoesNotAdvertiseDisabledConversationPersistence()
    {
        var endpoint = new Endpoint(
            MicrosoftOptions.Create(new AIHostOptions { ConversationPersistenceEnabled = false }),
            [new TestAIProvider()],
            [new TestConversationStore()],
            [new TestProposalStore()],
            CreateScopeFactory());

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        Assert.False(response.ConversationPersistence);
    }

    private static IServiceScopeFactory CreateScopeFactory() =>
        new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

    private class TestAIProvider(string name = "test") : IAIProvider
    {
        public string Name => name;

        public ValueTask<AISessionHandle> CreateSessionAsync(CreateAISessionRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AISessionHandle { Id = request.ConversationId });

        public async IAsyncEnumerable<AIProviderEvent> ExecuteTurnAsync(AITurnRequest request, IAIProviderToolInvoker toolInvoker, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield break;
        }
    }

    private class TestConversationStore : IAIConversationStore
    {
        public ValueTask<AIConversation?> FindAsync(string id, CancellationToken cancellationToken = default) => ValueTask.FromResult<AIConversation?>(null);
        public ValueTask SaveAsync(AIConversation conversation, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    private class TestProposalStore : IAIProposalStore
    {
        public ValueTask<AIProposal?> FindAsync(string id, string? tenantId, CancellationToken cancellationToken = default) => ValueTask.FromResult<AIProposal?>(null);
        public ValueTask SaveAsync(AIProposal proposal, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
