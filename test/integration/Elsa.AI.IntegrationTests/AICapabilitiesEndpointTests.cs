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
    [Test]
    [DisplayName("Capabilities endpoint advertises Weaver MVP capabilities")]
    public async Task CapabilitiesEndpointAdvertisesWeaverMvpCapabilities()
    {
        using var scopeProvider = new ServiceCollection().BuildServiceProvider();
        var endpoint = new Endpoint(
            MicrosoftOptions.Create(new AIHostOptions { ConversationPersistenceEnabled = true }),
            [new TestAIProvider()],
            [new TestConversationStore()],
            [new TestProposalStore()],
            scopeProvider.GetRequiredService<IServiceScopeFactory>());

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        await Assert.That(response.Streaming).IsTrue();
        await Assert.That(response.ConversationPersistence).IsTrue();
        await Assert.That(response.ProposalReview).IsTrue();
        await Assert.That(response.SupportedAttachmentKinds).Contains("WorkflowDefinition");
        await Assert.That(response.SupportedAttachmentKinds).Contains("WorkflowInstance");
        await Assert.That(response.SupportedAttachmentKinds).Contains("Activity");
        await Assert.That(response.SupportedAttachmentKinds).Contains("DiagnosticsScope");
        await Assert.That(response.SupportedAttachmentKinds).Contains("TimeRange");
        await Assert.That(response.Grounding).Contains(x => x.Family == "activities");
        await Assert.That(response.Grounding).Contains(x => x.Family == "workflows");
        await Assert.That(response.Grounding).Contains(x => x.Family == "proposals");
        await Assert.That(response.Grounding).Contains(x => x.Family == "runtime");
    }

    [Test]
    [DisplayName("Capabilities endpoint hides unavailable capabilities")]
    public async Task CapabilitiesEndpointHidesUnavailableCapabilities()
    {
        using var scopeProvider = new ServiceCollection().BuildServiceProvider();
        var endpoint = new Endpoint(
            MicrosoftOptions.Create(new AIHostOptions { StreamingEnabled = false, ConversationPersistenceEnabled = true }),
            [new TestAIProvider()],
            [new TestConversationStore()],
            [],
            scopeProvider.GetRequiredService<IServiceScopeFactory>());

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        await Assert.That(response.Streaming).IsFalse();
        await Assert.That(response.ConversationPersistence).IsTrue();
        await Assert.That(response.ProposalReview).IsFalse();
    }

    [Test]
    [DisplayName("Capabilities endpoint hides streaming when multiple providers need a default")]
    public async Task CapabilitiesEndpointHidesStreamingWhenMultipleProvidersNeedADefault()
    {
        using var scopeProvider = new ServiceCollection().BuildServiceProvider();
        var endpoint = new Endpoint(
            MicrosoftOptions.Create(new AIHostOptions()),
            [new TestAIProvider("provider-1"), new TestAIProvider("provider-2")],
            [new TestConversationStore()],
            [],
            scopeProvider.GetRequiredService<IServiceScopeFactory>());

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        await Assert.That(response.Streaming).IsFalse();
    }

    [Test]
    [DisplayName("Capabilities endpoint advertises streaming when configured default provider resolves")]
    public async Task CapabilitiesEndpointAdvertisesStreamingWhenConfiguredDefaultProviderResolves()
    {
        using var scopeProvider = new ServiceCollection().BuildServiceProvider();
        var endpoint = new Endpoint(
            MicrosoftOptions.Create(new AIHostOptions { DefaultProviderName = "provider-2" }),
            [new TestAIProvider("provider-1"), new TestAIProvider("provider-2")],
            [new TestConversationStore()],
            [],
            scopeProvider.GetRequiredService<IServiceScopeFactory>());

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        await Assert.That(response.Streaming).IsTrue();
    }

    [Test]
    [DisplayName("Capabilities endpoint advertises registered durable conversation persistence")]
    public async Task CapabilitiesEndpointAdvertisesRegisteredDurableConversationPersistence()
    {
        using var scopeProvider = new ServiceCollection().BuildServiceProvider();
        var endpoint = new Endpoint(
            MicrosoftOptions.Create(new AIHostOptions()),
            [new TestAIProvider()],
            [new TestConversationStore()],
            [],
            scopeProvider.GetRequiredService<IServiceScopeFactory>());

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        await Assert.That(response.ConversationPersistence).IsTrue();
    }

    [Test]
    [DisplayName("Capabilities endpoint does not advertise in-memory conversation persistence")]
    public async Task CapabilitiesEndpointDoesNotAdvertiseInMemoryConversationPersistence()
    {
        using var scopeProvider = new ServiceCollection().BuildServiceProvider();
        var endpoint = new Endpoint(
            MicrosoftOptions.Create(new AIHostOptions { ConversationPersistenceEnabled = true }),
            [new TestAIProvider()],
            [new InMemoryAIConversationStore()],
            [new TestProposalStore()],
            scopeProvider.GetRequiredService<IServiceScopeFactory>());

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        await Assert.That(response.ConversationPersistence).IsFalse();
    }

    [Test]
    [DisplayName("Capabilities endpoint does not advertise disabled conversation persistence")]
    public async Task CapabilitiesEndpointDoesNotAdvertiseDisabledConversationPersistence()
    {
        using var scopeProvider = new ServiceCollection().BuildServiceProvider();
        var endpoint = new Endpoint(
            MicrosoftOptions.Create(new AIHostOptions { ConversationPersistenceEnabled = false }),
            [new TestAIProvider()],
            [new TestConversationStore()],
            [new TestProposalStore()],
            scopeProvider.GetRequiredService<IServiceScopeFactory>());

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        await Assert.That(response.ConversationPersistence).IsFalse();
    }

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
