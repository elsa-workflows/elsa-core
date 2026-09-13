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

    [Test]
    public async Task ADeliveryRoundTripsItsSecretAndItsRoutingMetadata()
    {
        await ActivateAsync();
        var delivery = Delivery(token: "s3cret-token", recipient: "guest@example.com");
        await Outbox.EnqueueAsync(delivery);

        var dequeued = await Assert.That(await DequeueMineAsync()).HasSingleItem();

        await Assert.That(dequeued.Id).IsEqualTo(delivery.Id);
        await Assert.That(dequeued.Token).IsEqualTo("s3cret-token");
        await Assert.That(dequeued.Recipient).IsEqualTo("guest@example.com");
        await Assert.That(dequeued.TaskId).IsEqualTo(delivery.TaskId);
        await Assert.That(dequeued.InvitationId).IsEqualTo(delivery.InvitationId);
        await Assert.That(dequeued.DispatcherName).IsEqualTo(delivery.DispatcherName);
    }

    [Test]
    public async Task ACompletedDeliveryIsRemovedSoTheSecretStopsExisting()
    {
        await ActivateAsync();
        var delivery = Delivery();
        await Outbox.EnqueueAsync(delivery);

        await Outbox.CompleteAsync(delivery.Id);

        await Assert.That(await DequeueMineAsync()).IsEmpty();
    }

    [Test]
    public async Task ADeliveryIsNotDueBeforeItsScheduledTime()
    {
        await ActivateAsync();
        var delivery = Delivery(notBefore: Clock.UtcNow.AddMinutes(10));
        await Outbox.EnqueueAsync(delivery);

        await Assert.That(await DequeueMineAsync()).IsEmpty();

        Clock.Advance(TimeSpan.FromMinutes(11));
        await Assert.That(await DequeueMineAsync()).HasSingleItem();
    }

    [Test]
    public async Task ReschedulingAdvancesTheAttemptCountAndDefersTheDelivery()
    {
        await ActivateAsync();
        Fixture.Settings.InvitationDeliveryRetryDelays = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5)];
        var delivery = Delivery();
        await Outbox.EnqueueAsync(delivery);

        await Outbox.RescheduleAsync(delivery.Id, Clock.UtcNow.AddMinutes(1));
        await Assert.That(await DequeueMineAsync()).IsEmpty();

        Clock.Advance(TimeSpan.FromMinutes(2));
        var retried = await Assert.That(await DequeueMineAsync()).HasSingleItem();
        await Assert.That(retried.Attempt).IsEqualTo(1);
    }

    [Test]
    public async Task DeliveryIsAbandonedOnceTheRetryScheduleIsExhausted()
    {
        await ActivateAsync();
        Fixture.Settings.InvitationDeliveryRetryDelays = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5)];
        var delivery = Delivery();
        await Outbox.EnqueueAsync(delivery);

        // One reschedule per configured delay is still retryable; the one past the end abandons.
        foreach (var _ in Fixture.Settings.InvitationDeliveryRetryDelays)
            await Outbox.RescheduleAsync(delivery.Id, Clock.UtcNow);
        await Assert.That(await DequeueMineAsync()).HasSingleItem();

        await Outbox.RescheduleAsync(delivery.Id, Clock.UtcNow);

        // An undeliverable secret expires rather than being retried forever; a manager reissues instead.
        await Assert.That(await DequeueMineAsync()).IsEmpty();
    }

    [Test]
    public async Task ReschedulingAnUnknownDeliveryIsHarmless()
    {
        await ActivateAsync();

        await Outbox.RescheduleAsync($"delivery-{Guid.NewGuid():N}", Clock.UtcNow);

        await Assert.That(await DequeueMineAsync()).IsEmpty();
    }

    [Test]
    public async Task AnExpiredDeliveryIsDroppedRatherThanDeliveredLate()
    {
        await ActivateAsync();
        var delivery = Delivery(expiresAt: Clock.UtcNow.AddMinutes(5));
        await Outbox.EnqueueAsync(delivery);

        Clock.Advance(TimeSpan.FromMinutes(6));

        await Assert.That(await DequeueMineAsync()).IsEmpty();
        // And it stays gone: a later sweep must not resurrect a secret whose invitation has expired.
        Clock.Advance(TimeSpan.FromMinutes(-6));
        await Assert.That(await DequeueMineAsync()).IsEmpty();
    }

    [Test]
    public async Task TheDueBatchIsBoundedByTheRequestedCount()
    {
        await ActivateAsync();
        foreach (var _ in Enumerable.Range(0, 3))
            await Outbox.EnqueueAsync(Delivery());

        var batch = await Outbox.DequeueDueAsync(1);

        await Assert.That(batch).HasSingleItem();
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
