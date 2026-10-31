using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Persistence.ConformanceTests.Infrastructure;
using Elsa.UserTasks.Persistence.ConformanceTests.Providers;

namespace Elsa.UserTasks.Persistence.ConformanceTests;

/// <summary>
/// The behaviour every <see cref="IUserTaskInvitationOutbox"/> owes its callers. The outbox holds the only
/// copy of an invitation secret between issuance and delivery, so "delivered late" and "retried forever"
/// are both security outcomes, not just reliability ones.
/// </summary>
public abstract class UserTaskInvitationOutboxConformanceTests(UserTaskStoreFixture fixture) : UserTaskConformanceTestBase(fixture)
{
    private readonly HashSet<string> _mine = new(StringComparer.Ordinal);

    private IUserTaskInvitationOutbox Outbox => Fixture.Outbox;

    [ConformanceFact]
    public async Task ADeliveryRoundTripsItsSecretAndItsRoutingMetadata()
    {
        await ActivateAsync();
        var delivery = Delivery(token: "s3cret-token", recipient: "guest@example.com");
        await Outbox.EnqueueAsync(delivery);

        var dequeued = Assert.Single(await DequeueMineAsync());

        Assert.Equal(delivery.Id, dequeued.Id);
        Assert.Equal("s3cret-token", dequeued.Token);
        Assert.Equal("guest@example.com", dequeued.Recipient);
        Assert.Equal(delivery.TaskId, dequeued.TaskId);
        Assert.Equal(delivery.InvitationId, dequeued.InvitationId);
        Assert.Equal(delivery.DispatcherName, dequeued.DispatcherName);
    }

    [ConformanceFact]
    public async Task ACompletedDeliveryIsRemovedSoTheSecretStopsExisting()
    {
        await ActivateAsync();
        var delivery = Delivery();
        await Outbox.EnqueueAsync(delivery);

        await Outbox.CompleteAsync(delivery.Id);

        Assert.Empty(await DequeueMineAsync());
    }

    [ConformanceFact]
    public async Task ADeliveryIsNotDueBeforeItsScheduledTime()
    {
        await ActivateAsync();
        var delivery = Delivery(notBefore: Clock.UtcNow.AddMinutes(10));
        await Outbox.EnqueueAsync(delivery);

        Assert.Empty(await DequeueMineAsync());

        Clock.Advance(TimeSpan.FromMinutes(11));
        Assert.Single(await DequeueMineAsync());
    }

    [ConformanceFact]
    public async Task ReschedulingAdvancesTheAttemptCountAndDefersTheDelivery()
    {
        await ActivateAsync();
        Fixture.Settings.InvitationDeliveryRetryDelays = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5)];
        var delivery = Delivery();
        await Outbox.EnqueueAsync(delivery);

        await Outbox.RescheduleAsync(delivery.Id, Clock.UtcNow.AddMinutes(1));
        Assert.Empty(await DequeueMineAsync());

        Clock.Advance(TimeSpan.FromMinutes(2));
        var retried = Assert.Single(await DequeueMineAsync());
        Assert.Equal(1, retried.Attempt);
    }

    [ConformanceFact]
    public async Task DeliveryIsAbandonedOnceTheRetryScheduleIsExhausted()
    {
        await ActivateAsync();
        Fixture.Settings.InvitationDeliveryRetryDelays = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5)];
        var delivery = Delivery();
        await Outbox.EnqueueAsync(delivery);

        // One reschedule per configured delay is still retryable; the one past the end abandons.
        foreach (var _ in Fixture.Settings.InvitationDeliveryRetryDelays)
            await Outbox.RescheduleAsync(delivery.Id, Clock.UtcNow);
        Assert.Single(await DequeueMineAsync());

        await Outbox.RescheduleAsync(delivery.Id, Clock.UtcNow);

        // An undeliverable secret expires rather than being retried forever; a manager reissues instead.
        Assert.Empty(await DequeueMineAsync());
    }

    [ConformanceFact]
    public async Task ReschedulingAnUnknownDeliveryIsHarmless()
    {
        await ActivateAsync();

        await Outbox.RescheduleAsync($"delivery-{Guid.NewGuid():N}", Clock.UtcNow);

        Assert.Empty(await DequeueMineAsync());
    }

    [ConformanceFact]
    public async Task AnExpiredDeliveryIsDroppedRatherThanDeliveredLate()
    {
        await ActivateAsync();
        var delivery = Delivery(expiresAt: Clock.UtcNow.AddMinutes(5));
        await Outbox.EnqueueAsync(delivery);

        Clock.Advance(TimeSpan.FromMinutes(6));

        Assert.Empty(await DequeueMineAsync());
        // And it stays gone: a later sweep must not resurrect a secret whose invitation has expired.
        Clock.Advance(TimeSpan.FromMinutes(-6));
        Assert.Empty(await DequeueMineAsync());
    }

    [ConformanceFact]
    public async Task TheDueBatchIsBoundedByTheRequestedCount()
    {
        await ActivateAsync();
        foreach (var _ in Enumerable.Range(0, 3))
            await Outbox.EnqueueAsync(Delivery());

        var batch = await Outbox.DequeueDueAsync(1);

        Assert.Single(batch);
    }

    /// <summary>
    /// Dequeues and keeps only this test's own entries. <c>DequeueDueAsync</c> is deliberately not
    /// tenant-scoped — the worker drains the whole host — so filtering here is what isolates the test.
    /// </summary>
    private async Task<IReadOnlyList<UserTaskInvitationDelivery>> DequeueMineAsync() =>
        (await Outbox.DequeueDueAsync(500)).Where(x => _mine.Contains(x.Id)).ToList();

    private UserTaskInvitationDelivery Delivery(
        string token = "invitation-token",
        string? recipient = null,
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? notBefore = null)
    {
        var id = $"delivery-{Guid.NewGuid():N}";
        _mine.Add(id);
        return new(id, TenantId, $"task-{Guid.NewGuid():N}", $"invitation-{Guid.NewGuid():N}", "bearer", token,
            expiresAt ?? Clock.UtcNow.AddDays(1))
        {
            Recipient = recipient,
            NotBefore = notBefore
        };
    }
}
