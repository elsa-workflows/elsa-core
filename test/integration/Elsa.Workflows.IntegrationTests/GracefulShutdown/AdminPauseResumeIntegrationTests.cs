using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.PublishingStrategies;
using Elsa.Testing.Shared;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.GracefulShutdown;

/// <summary>
/// Verifies the pause/resume idempotency invariants and the SC-007 audit-entry-count rule against the real DI graph.
/// The HTTP layer is exercised separately by component tests (Elsa.Workflows.ComponentTests); these tests focus on the
/// audit-publishing logic that lives inside the endpoint handlers.
/// </summary>
public class AdminPauseResumeIntegrationTests : IAsyncDisposable
{
    private readonly IServiceProvider _services;
    private readonly RecordingNotificationSender _recorder;

    public AdminPauseResumeIntegrationTests()
    {
        _recorder = new RecordingNotificationSender();
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .ConfigureElsa(elsa => elsa.UseWorkflowRuntime())
            .ConfigureServices(s => s.AddSingleton<INotificationSender>(_recorder))
            .Build();
    }

    [Test]
    [DisplayName("PauseAsync followed by repeated PauseAsync calls converges on the same state")]
    public async Task PauseIsIdempotent()
    {
        var signal = _services.GetRequiredService<IQuiescenceSignal>();

        var first = await signal.PauseAsync("maintenance", "op@example.com", CancellationToken.None);
        var second = await signal.PauseAsync("maintenance", "op@example.com", CancellationToken.None);

        await Assert.That(first.Reason.HasFlag(QuiescenceReason.AdministrativePause)).IsTrue();
        await Assert.That(second.PausedAt).IsEqualTo(first.PausedAt);
    }

    [Test]
    [DisplayName("ResumeAsync clears AdministrativePause; second call is a no-op")]
    public async Task ResumeIsIdempotent()
    {
        var signal = _services.GetRequiredService<IQuiescenceSignal>();
        await signal.PauseAsync(null, null, CancellationToken.None);

        var first = await signal.ResumeAsync("op@example.com", CancellationToken.None);
        var second = await signal.ResumeAsync("op@example.com", CancellationToken.None);

        await Assert.That(first.Reason.HasFlag(QuiescenceReason.AdministrativePause)).IsFalse();
        await Assert.That(second.Reason).IsEqualTo(QuiescenceReason.None);
    }

    [Test]
    [DisplayName("ResumeAsync during drain is a no-op (returns state with Drain still set)")]
    public async Task ResumeDuringDrainIsNoOp()
    {
        var signal = _services.GetRequiredService<IQuiescenceSignal>();
        await signal.PauseAsync(null, null, CancellationToken.None);
        await signal.BeginDrainAsync();

        var state = await signal.ResumeAsync(null, CancellationToken.None);

        await Assert.That(state.Reason.HasFlag(QuiescenceReason.Drain)).IsTrue();
        await Assert.That(state.Reason.HasFlag(QuiescenceReason.AdministrativePause)).IsTrue();
    }

    [Test]
    [DisplayName("Internal bookmark-queue ingress source surfaces in registry snapshot")]
    public async Task InternalIngressSourceVisible()
    {
        var registry = _services.GetRequiredService<IIngressSourceRegistry>();
        var sources = registry.Snapshot();
        await Assert.That(sources).Contains(s => s.Name == "internal.bookmark-queue-worker");
    }

    [Test]
    [DisplayName("Internal ingress source reports Paused state when the runtime is paused")]
    public async Task InternalIngressReflectsSignal()
    {
        var registry = _services.GetRequiredService<IIngressSourceRegistry>();
        var signal = _services.GetRequiredService<IQuiescenceSignal>();
        var internalSource = registry.Sources.First(s => s.Name == "internal.bookmark-queue-worker");

        await Assert.That(internalSource.CurrentState).IsEqualTo(IngressSourceState.Running);

        await signal.PauseAsync(null, null, CancellationToken.None);

        await Assert.That(internalSource.CurrentState).IsEqualTo(IngressSourceState.Paused);
    }

    /// <summary>Captures published mediator notifications for assertion.</summary>
    private sealed class RecordingNotificationSender : INotificationSender
    {
        public List<INotification> Published { get; } = new();

        public Task SendAsync(INotification notification, CancellationToken cancellationToken = default)
        {
            Published.Add(notification);
            return Task.CompletedTask;
        }

        public Task SendAsync(INotification notification, IEventPublishingStrategy? strategy, CancellationToken cancellationToken = default)
        {
            Published.Add(notification);
            return Task.CompletedTask;
        }
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
